using cryptoprime;
using vinkekfish;
using main_tests;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Threading;

namespace permutationsTest
{
/*
 * Тест касается разработки расширенного алгоритма на 4096 битный ключ
 * 
 * Этот тест пытается расчитать по упрощённой имитационной модели, сколько нужно перестановок и преобразований для того,
 * чтобы обеспечить диффузию между всеми байтами криптографического состояния расширенного алгоритма
 * 
 * Константа K - коэффициент умножения размера внутреннего состояния VinKekFish.
 * Запуск теста осуществляется при различных константах K. Величину константы K нужно устанавливать вручную.
 * 
 * Результаты работы по диффузии:
 *  K = 1;
 *      Min = 0,9983984, Max = 1,023999;  3 итерации (3 раунда по одной перестановке keccak)
 *      Min = 0,9994209, Max = 1,000446;  4 итерации
 *      Min = 0,9999946, Max = 1,000036;  5 итераций
 *      Min = 0,9999981, Max = 0,9999997; 6 итераций (выполняется 1 мин или 23 сек в многопоточном режиме)
 * 
 * K = 3;
 *      Min = 0,9575994, Max = 1,055999; 3 итерации
 *      Min = 0,9986629, Max = 1,001394; 4 итерации
 *      Min = 0,9999809, Max = 1,00002;  5 итераций
 *      Min = 0,999998,  Max = 1,000001; 6 итераций (выполняется 7:22 (мин:сек) в многопоточном режиме с распараллеливанием в ProbabilityImitationForKeccak; за 2:25 с распараллеливанием в doPermutationTest_Keccak )
 * 
 * K = 5;
 *      Min = 0,9159989, Max = 1,107999; 3 итерации
 *      Min = 0,9970089, Max = 1,002639; 4 итерации
 *      Min = 0,999954,  Max = 1,000052; 5 итераций
 *      Min = 0,9999971, Max = 1,000001; 6 итераций
 *
 * K = 6;
 *      Min = 0,997619,  Max = 1,002612; 4 итерации
 * 		Min = 0,9999442, Max = 1,000062; 5 итераций
 * 		Min = 0,9999937, Max = 1,000003; 6 итераций
 * 
 * K = 7;
 *      Min = 0,9463994, Max = 1,0612;   3 итерации
 *		Min = 0,9975408, Max = 1,003058; 4 итерации
 * 		Min = 0,9999342, Max = 1,000081; 5 итераций
 *
 * */
    unsafe class PermutationDiffusionTestx5: MultiThreadTest<PermutationDiffusionTestx5.SourceTask>
    {
        public const int K = 6;
        public PermutationDiffusionTestx5(ConcurrentQueue<TestTask> tasks): base(tasks, "PermutationDiffusionTestx5", new PermutationDiffusionTestx5.SourceTaskFabric())
        {
            sizeInBits = 25600*K;
            size       = sizeInBits >> 3;
        }

        public new class SourceTask: MultiThreadTest<SourceTask>.SourceTask
        {
            public readonly string        TableName = null;
            public readonly int           IterationCount;
            public readonly CipherOptions options;

            public readonly MultiThreadTest<PermutationDiffusionTestx5.SourceTask> task;

            public SourceTask(int iterationCount, CipherOptions opt, string TableName, MultiThreadTest<PermutationDiffusionTestx5.SourceTask> task)
            {
                this.TableName      = TableName;
                this.IterationCount = iterationCount;
                this.options        = opt;
                this.task           = task;
            }
        }

        public new class SourceTaskFabric: MultiThreadTest<SourceTask>.SourceTaskFabric
        {
            public override IEnumerable<SourceTask> GetIterator()
            {
                foreach (var opt in /*CipherOptions.getAllNotEmptyCiphers()*/ CipherOptions.getKeccakCipher())
                {
                    // 6 минут на 1 раунд keccak
                    foreach (int i in new int[] {8/*, 12, 15*/})
                    {
                        yield return new SourceTask(i, opt, "transpose128", this.task);
                        /*yield return new SourceTask(i, opt, "transpose200");
                        yield return new SourceTask(i, opt, "transpose256");
                        yield return new SourceTask(i, opt, "transpose384");
                        yield return new SourceTask(i, opt, "transpose387");*/
                    }
                }
            }
        }

        public class CipherOptions
        {
            public readonly bool useThreeFish = true;
            public readonly bool useKeccak    = true;

            public bool useThreeFishOnly  { get =>  useThreeFish && !useKeccak; }
            public bool useKeccakOnly     { get => !useThreeFish &&  useKeccak; }

            public CipherOptions(bool useThreeFish = true, bool useKeccak = true)
            {
                this.useThreeFish = useThreeFish;
                this.useKeccak    = useKeccak;
            }

            public override string ToString()
            {
                var str = "";
                if (useKeccak)
                    str += "keccak";

                if (useThreeFish)
                {
                    if (useKeccak)
                        str += "+threefish";
                    else
                        str += "threefish";
                }

                return str;
            }

            public static IEnumerable<CipherOptions> getAllNotEmptyCiphers()
            {
                yield return new CipherOptions(true,  false);
                yield return new CipherOptions(false, true);
                yield return new CipherOptions(true,  true);
            }

            public static IEnumerable<CipherOptions> getKeccakCipher()
            {
                // yield return new CipherOptions(true,  false);
                yield return new CipherOptions(false, true);
                // yield return new CipherOptions(true,  true);
            }
        }

        public unsafe override void StartTests()
        {
            GenerateTables();

            var po = new ParallelOptions();
            po.MaxDegreeOfParallelism = 1; // Environment.ProcessorCount; // начинается OutOfMemory
            Parallel.ForEach
            (
                sources, po,
                delegate (SourceTask task)
                {
                    try
                    {
                        /*
                            * Идея теста
                            * Каждый байт должен повлиять на каждый другой байт
                            * Если N байтов идут в перестановке, это означает, что с вероятностью 1/N эти байты повлияют друг на друга
                            * Нужно, чтобы байты влияли друг на друга во всём состоянии примерно с вероятностью 1/N
                            * И нужно, чтобы ВСЕ байты влияли друг на друга
                            * Кроме этого, двойное влияние также можно зачесть в отдельной статистике
                            * 
                            * Общий алгоритм теста
                            * 
                            * P[i, j] есть "вероятность" того, что на байт j будет оказано влияние от байта i
                            * 
                            * На каждый байт у нас, по сути, должно быть два массива влияния:
                            * один на равномерность влияния: вероятность влияния каждого байта на этот
                            * второй на общее количество влияния: вероятность влияния умноженная на количество влияний
                            * 
                            * Таким образом, у нас есть массив P и массив F из чисел float
                            * 
                            * Изначально каждый байт номер I содержит 1f на месте байта I, и 0f на местах других байтов
                            * Каждый стоблец номер I описывает влияние других байтов на I
                            * При перестановках байтов столбцы не переставляются. Перестановки записываются в массив перестановок R[i]
                            * Таким образом, для учёта статистики по байту, находящемуся на позиции i, необходимо учитывать её в F и P на позиции R[i]
                            * Если бы перестановок не было, то индексы бы совпадали
                            * 
                            * 
                            * При каждом шаге перестановок происходит следующее:
                            *  для массива P
                            *      создать вспомогательный массив с нулями // это неверно и скопировать из него столбец J (столбец с номером указанного байта)
                            *      // это неверно: при копировании сразу разделить содержимое массива на два и на blockSize, т.к. влияние теперь будет и от других байтов
                            *      для блока размером blockSize байтов
                            *          для каждого байта I из блока, включая I = J
                            *              сложить всопогательный массив столбца J со значениями из массива I, но делёными на blockSize и ещё на два
                            * 
                            * После проведения операций по всем байтам состояния, скопировать вспомогательные массивы в основные (в P или F)
                        * */

                        // Создаём массив P с равномерностью влияния байтов и инициализируем его
                        var P = new float [size, size];
                        var H = new float [size, size]; // вспомогательный массив
                        var R = new ushort[size];       // Массив с номерами байтов после перестановок

                        for (ushort i = 0; i < size; i++)
                        {
                            R[i] = i;
                            for (int j = 0; j < size; j++)
                            {
                                P[i, j] = 0f;

                                if (i == j)
                                {
                                    P[i, j] = 1f;
                                }
                            }
                        }

                        // doPermutationTest(P, H, R, task.IterationCount, task.TableName, task.options);
            
                        // Делаем это вместо doPermutationTest - только keccak
                        for (int iterationNumber = 1; iterationNumber <= task.IterationCount; iterationNumber++)
                        {
                            doPermutationTest_Keccak(P, H, R);
                            DoPermutation(R, tables[task.TableName]);

                            if (iterationNumber < task.IterationCount - 4 || iterationNumber < 3)
                                continue;

                            var Result = new float[P.GetLength(0), P.GetLength(1)];
                            // Копируем матрицу в матрицу результата и сразу же нормализуем её
                            for (ushort i = 0; i < size; i++)
                            {
                                for (int j = 0; j < size; j++)
                                {
                                    // P[i, j] *= (float) size;
                                    Result[i, j] = P[i, j] * (float) size;
                                }
                            }

                            // Проверяем, что всё в пределах нормы
                            bool error = false;
                            float Max = float.MinValue, Min = float.MaxValue;
                            for (ushort i = 0; i < size; i++)
                            {
                                for (int j = 0; j < size; j++)
                                {
                                    if (Result[i, j] > 1.001 || Result[i, j] < 0.999)
                                    {
                                        /*if (!error)
                                            this.task.error.Add(new Error() {Message = $"P[i, j] > 1.001 || P[i, j] < 0.99: P[{i}, {j}] == {P[i, j]}"});*/

                                        error = true;
                                        // goto @break;
                                    }

                                    if (Result[i, j] > Max)
                                        Max = Result[i, j];
                                    if (Result[i, j] < Min)
                                        Min = Result[i, j];
                                }
                            }
                            // @break:

                            var sb = new StringBuilder();
                            /*var ss = 1000f;
                            sb.AppendLine("P martix * " + ss);
                            if (error)
                                sb.AppendLine("P[i, j] > 1.001 || P[i, j] < 0.99");
                            sb.AppendLine($"Min = {Min}, Max = {Max}");

                            for (ushort i = 0; i < size; i++)
                            {
                                for (int j = 0; j < size; j++)
                                {
                                    sb.Append(((int)(Result[i, j]*ss)).ToString("D3") + "\t");
                                }

                                sb.AppendLine();
                            }
                            
                            File.WriteAllText($"matrix/matrix-{task.options.ToString()}-{task.TableName}-{task.IterationCount.ToString("D2")}.txt", sb.ToString());
                            */

                            File.WriteAllText($"matrix/r-x{K.ToString()}-{task.options.ToString()}-{task.TableName}-{iterationNumber.ToString("D2")}.txt", $"Min = {Min}, Max = {Max}");
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (task.task.task)
                        {
                            var error = new Error();
                            error.ex = ex;
                            error.Message = ex.Message + "\r\n" + ex.StackTrace;
                            task.task.task.error.Add(error);
                        }
                    }
                }
            );  // The end of Parallel.foreach sources running
        }

        private void doPermutationTest(float[,] P, float[,] H, ushort[] R, int k, string TableName, CipherOptions options)
        {
            // doPermutationTest_Keccak   (P, F, H, R);
            if (options.useThreeFishOnly)
            {
                for (int i = 0; i < k; i++)
                {
                    doPermutationTest_Threefish(P, H, R);
                    DoPermutation(R, tables[TableName]);
                }
            }
            else
            if (options.useKeccakOnly)
            {
                for (int i = 0; i < k; i++)
                {
                    doPermutationTest_Keccak(P, H, R);
                    DoPermutation(R, tables[TableName]);
                }
            }
            else
            {
                for (int i = 0; i < k; i++)
                {
                    if (options.useKeccak)
                    {
                        doPermutationTest_Keccak(P, H, R);
                        DoPermutation(R, tables[TableName]);
                    }

                    if (options.useThreeFish)
                    {
                        doPermutationTest_Threefish(P, H, R);
                        // DoPermutation(R, tables["transpose387"]);
                        DoPermutation(R, tables["transpose128"]);
                    }
                }
            }
        }

        public static int getNumberFromRing(int i, int ringModulo)
        {
            while (i < 0)
                i += ringModulo;

            while (i >= ringModulo)
                i -= ringModulo;

            return i;
        }
        
        private void CopyToH(float[,] P, float[,] H, float divide)
        {
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    H[i, j] = P[i, j] / divide;
                }
            }
        }

        /// <summary>Осуществляет имитацию преобразования Threefish (изменяет статистику)</summary>
        private void doPermutationTest_Threefish(float[,] P, float[,] h, ushort[] R)
        {
            var size    = R.Length;
            var size128 = size / 128;
            var size256 = size128 / 2;
            
            var PH = new float [size, size]; // вспомогательный массив
            // Работаем с массивом P
            CopyToH(P, PH, 1f);

            for (int i = 0; i < size128; i++)
            {
                ThreeFishImitation(P, PH, h, R, i, getNumberFromRing(i + size256, size128), size128);
            }

            CopyToH(PH, P, 1f);
        }

        private void ThreeFishImitation(float[,] P, float[,] PH, float[,] H, ushort[] R, int blockPosition, int keyPosition, int size128)
        {
            int startB = blockPosition * 128;
            int startK = keyPosition * 128;

            CopyToH(PH, H,  1f);
            // MatrixToNull(H);

            // P[i, j] есть "вероятность" того, что на байт j будет оказано влияние от байта i
            ProbabilityImitationForThreeFish(P, PH, H, R, startB, startK);

            CopyToH(H, PH, 1f);
        }

        private void MatrixToNull(float[,] H)
        {
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    H[i, j] = 0f;
                }
            }
        }

        private void ProbabilityImitationForThreeFish(float[,] P, float[,] PH, float[,] H, ushort[] R, int startB, int startK)
        {
            var kDiv = 1f / (256f);   // 256 значений всего участвует в данном преобразовании

            for (int i = 0; i < size; i++)
            {
                var iR = GetByteNumber(i, R);
                for (int k = startB; k < startB + 128; k++)
                {
                    H[iR, GetByteNumber(k, R)] = 0f;
                }
            }

            for (int i = 0; i < size; i++)
            {
                var iR = GetByteNumber(i, R);
                for (int j = startK; j < startK + 128; j++)
                for (int k = startB; k < startB + 128; k++)
                {
                    H[iR, GetByteNumber(k, R)] += P[iR, GetByteNumber(j, R)] * kDiv;
                }
            }

            for (int i = 0; i < size; i++)
            {
                var iR = GetByteNumber(i, R);
                for (int j = startB; j < startB + 128; j++)
                for (int k = startB; k < startB + 128; k++)
                {
                    H[iR, GetByteNumber(k, R)] += PH[iR, GetByteNumber(j, R)] * kDiv;
                }
            }
        }

        private void doPermutationTest_Keccak(float[,] P, float[,] H, ushort[] R)
        {
            var size    = R.Length;
            var size200 = size / 200;
            var size400 = size200 / 2;
            
            var PH = new float [size, size]; // вспомогательный массив
            // Работаем с массивом P
            CopyToH(P, PH, 1f);

            /*
            for (int i = 0; i < size200; i++)
            {
                KeccakImitation(P, PH, H, R, i, size200);
            }
            */

            CopyToH(PH, H,  1f);
            Parallel.For
            (
                0, size200,
                delegate(int i)
                {
                    KeccakImitation(P, PH, H, R, i, size200);
                }
            );
            CopyToH(H, PH, 1f);

            CopyToH(PH, P, 1f);
        }

        private void KeccakImitation(float[,] P, float[,] PH, float[,] H, ushort[] R, int blockPosition, int size128)
        {
            int startB = blockPosition * 200;

            //CopyToH(PH, H,  1f);
            // MatrixToNull(H);

            // P[i, j] есть "вероятность" того, что на байт j будет оказано влияние от байта i
            ProbabilityImitationForKeccak(P, PH, H, R, startB);

            //CopyToH(H, PH, 1f);
        }

        private void ProbabilityImitationForKeccak(float[,] P, float[,] PH, float[,] H, ushort[] R, int startB)
        {
            var kDiv = 1f / (200f);   // 200 значений всего участвует в данном преобразовании

            for (int i = 0; i < size; i++)
            {
                var iR = GetByteNumber(i, R);
                for (int k = startB; k < startB + 200; k++)
                {
                    H[iR, GetByteNumber(k, R)] = 0f;
                }
            }
            
            for (int i = 0; i < size; i++)
            {
                var iR = GetByteNumber(i, R);

                for (int j = startB; j < startB + 200; j++)
                for (int k = startB; k < startB + 200; k++)
                {
                    H[iR, GetByteNumber(k, R)] += PH[iR, GetByteNumber(j, R)] * kDiv;
                }
            }

            /*
            Parallel.For
            (
                0, size,
                delegate(int i)
                {
                    var iR = GetByteNumber(i, R);

                    var Hn = new float[H.GetLength(1)];
                    for (int j = 0; j < Hn.Length; j++)
                        Hn[j] = 0;

                    for (int j = startB; j < startB + 200; j++)
                    for (int k = startB; k < startB + 200; k++)
                    {
                        Hn[GetByteNumber(k, R)] += PH[iR, GetByteNumber(j, R)] * kDiv;
                    }

                    lock (H)
                        for (int j = 0; j < Hn.Length; j++)
                            H[iR, j] += Hn[j];
                }
            );*/
        }

        /// <summary>Возвращает номер байта, находящегося на позиции positionInArray</summary>
        /// <param name="positionInArray">Позиция в массиве, для которой нужно узнать, какой там байт располагается</param>
        /// <param name="R">Массив расположения байтов. В R[i] содержится номер столбца в таблицах P и F</param>
        /// <returns>Номер байта, находящегося на позиции positionInArray</returns>
        public static ushort GetByteNumber(int positionInArray, ushort[] R)
        {
            return R[positionInArray];
        }

        /// <summary>Делает виртуальные перестановки байтов</summary>
        /// <param name="R">Массив расположения байтов</param>
        /// <param name="table">Массив для перестановок new[i] = old[table[i]]</param>
        private void DoPermutation(ushort[] R, ushort[] table)
        {
            var buff = new ushort[R.Length];

            /*
             * Перестановка:
             * Теперь байт на позиции i мы переставляем на позицию table[i]
             * 
             * Сначала нам нужно узнать, какой байт на позиции i
             * Байт, указанный в table[i] переставлятся НА позицию i с позиции table[i]
             * */
            for (int i = 0; i < R.Length; i++)
            {
                buff[i] = GetByteNumber(table[i], R);
            }

            for (int i = 0; i < R.Length; i++)
            {
                R[i] = buff[i];
            }
        }

        static readonly SortedList<string, ushort[]> tables = new SortedList<string, ushort[]>(16);
        public void GenerateTables()
        {
            lock (tables)
            {
                if (!Directory.Exists("matrix"))
                    Directory.CreateDirectory("matrix");

                if (tables.Count > 0)
                    return;
                    /*
                for (int i = 1; i < valueToAdd.Length; i++)
                    GenBaseTable(i);
                    */
                GenTransposeTable(128);
                GenTransposeTable(200);
                GenTransposeTable(256);
                GenTransposeTable(800);
                // GenTransposeTable(384); // Чтобы значения брались не по порядку, но кратно 128-ми
                // GenTransposeTable(256+131); // 387
            }
        }

        // 2 и 5 исключены, т.к. являются простыми множителями размера 25600 криптографического состояния
        // Кроме этого, 2 никогда не даёт приращения младшему биту, что криптографически не очень верно
        public static readonly ushort[] valueToAdd = {1, 3, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199, 211, 223, 227, 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281, 283, 293, 307, 311, 313, 317, 331, 337, 347, 349, 353, 359, 367, 373, 379, 383, 389, 397, 401, 409, 419, 421, 431, 433, 439, 443, 449, 457, 461, 463, 467, 479, 487, 491, 499, 503, 509, 521, 523, 541, 547};
        public readonly int sizeInBits;
        public readonly int size;

        private void GenBaseTable(int numberOfTable)
        {
            // var blockSize  = 64;

            var newTable = new ushort[size];
            var buffer   = new ushort[size];

            // Заполняем таблицу исходными номерами
            for (ushort i = 0; i < newTable.Length; i++)
            {
                newTable[i] = i;
                buffer[i]   = i;
            }

            for (int z = 0; z < 1; z++)
            {
                ushort j = 0, k = 0;
                for (ushort i = 0; i < newTable.Length; i++)
                {
                    // Байт на позиции j - это байт, который указан в newTable[k]
                    // На нулевой итерации это просто k. То есть на позиции j встаёт байт с номером k
                    buffer[j++] = newTable[k];

                    k += (ushort) (/*blockSize + */valueToAdd[numberOfTable]);
                    while (k >= buffer.Length)
                    {
                        k -= (ushort)buffer.Length;
                    }
                }

                fixed (ushort* nt = newTable, buff = buffer)
                {
                    BytesBuilder.CopyTo(buffer.Length << 1, buffer.Length << 1, (byte*)buff, (byte*)nt);
                }
            }

            for (ushort i = 0; i < newTable.Length; i++)
            {
                if (!newTable.Contains(i))
                {
                    throw new Exception($"GenBaseTable: !newTable.Contains({i}) numberOfTable = {numberOfTable}");
                }
            }

            // В итоге мы получаем таблицу, где для каждого индекса i содержится индекс байта index=newTable[i] в исходной таблице
            // То есть, если у нас есть массивы old и new, то
            // new[i] = old[newTable[i]]
            tables.Add("base" + valueToAdd[numberOfTable], newTable);
        }

        private void GenTransposeTable(int blockSize)
        {
            var newTable  = new ushort[size];
            var buffer    = new ushort[size];

            // Заполняем таблицу исходными номерами
            for (ushort i = 0; i < newTable.Length; i++)
            {
                newTable[i] = i;
                buffer[i]   = i;
            }

            //for (int z = 0; z < 1; z++)
            {
                ushort j = 0, k = 0;
                for (ushort i = 0; i < newTable.Length; i++)
                {
                    // Байт на позиции j - это байт, который указан в newTable[k]
                    // На нулевой итерации это просто k. То есть на позиции j встаёт байт с номером k
                    buffer[j++] = newTable[k];

                    k += (ushort) blockSize;
                    while (k >= buffer.Length)
                    {
                        k -= (ushort)buffer.Length;
                        k++;
                    }
                }

                fixed (ushort* nt = newTable, buff = buffer)
                {
                    BytesBuilder.CopyTo(buffer.Length << 1, buffer.Length << 1, (byte*)buff, (byte*)nt);
                }
            }

            for (ushort i = 0; i < newTable.Length; i++)
            {
                if (!newTable.Contains(i))
                {
                    throw new Exception($"GenTransposeTable: !newTable.Contains({i}) numberOfTable = {blockSize}");
                }
            }

            // В итоге мы получаем таблицу, где для каждого индекса i содержится индекс байта index=newTable[i] в исходной таблице
            // То есть, если у нас есть массивы old и new, то
            // new[i] = old[newTable[i]]
            tables.Add("transpose" + blockSize, newTable);

            // if (blockSize == 387)
            {
                // Мы хотим найти все значения, которые есть в выходном результате (первые 512-ть битов)
                // и отобразить как они попадают в исходные (до перестановок) блоки
                var sb = new StringBuilder();
                for (int i = 0; i < newTable.Length; i++)
                {
                    // Первые 512-ть байтов
                    bool fl = false;
                    for (int j = 0; j < 512; j++)
                    {
                        // Если по индексу j располагается значение, которое до перестановки было на месте i, то мы нашли нужное для отображения значение
                        if (newTable[j] == i)
                        {
                            fl = true;
                            break;
                        }
                    }

                    if (fl)
                        sb.Append("1");
                    else
                        sb.Append("0");

                    if ((i & 7) == 7)
                        sb.Append("\r\n");
                }

                File.WriteAllText($"matrix-{blockSize}.txt", sb.ToString());
            }
        }
    }
}
