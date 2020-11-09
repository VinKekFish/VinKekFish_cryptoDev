using cryptoprime;
using vinkekfish;
using main_tests;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;

namespace permutationsTest
{
/*
 * Тест касается разработки расширенного алгоритма на 4096 битный ключ
 * 
 * Этот тест пытается расчитать по упрощённой имитационной модели, сколько нужно перестановок и преобразований для того,
 * чтобы обеспечить диффузию между всеми байтами криптографического состояния расширенного алгоритма
 * */
    unsafe class PermutationDiffusionTest: MultiThreadTest<PermutationDiffusionTest.SourceTask>
    {
        public PermutationDiffusionTest(ConcurrentQueue<TestTask> tasks): base(tasks, "PermutationDiffusionTest", new PermutationDiffusionTest.SourceTaskFabric())
        {
            sizeInBits = 25600;
            size       = sizeInBits >> 3;
        }

        public new class SourceTask: MultiThreadTest<SourceTask>.SourceTask
        {
            public readonly string TableName = null;
            public readonly int    IterationCount;

            public SourceTask(int iterationCount, string TableName)
            {
                this.TableName      = TableName;
                this.IterationCount = iterationCount;
            }
        }

        public new class SourceTaskFabric: MultiThreadTest<SourceTask>.SourceTaskFabric
        {
            public override IEnumerable<SourceTask> GetIterator()
            {
                for (int i = 1; i <= 8; i++)
                {
                    yield return new SourceTask(i, "base131");
                    yield return new SourceTask(i, "transpose128");
                    yield return new SourceTask(i, "transpose200");
                }
            }
        }

        public unsafe override void StartTests()
        {
            GenerateTables();

            var po = new ParallelOptions();
            po.MaxDegreeOfParallelism = Environment.ProcessorCount;
            Parallel.ForEach
            (
                sources, po,
                delegate (SourceTask task)
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

                    doPermutationTest(P, H, R, task.IterationCount, task.TableName);
            
                    // Нормализуем матрицу
                    for (ushort i = 0; i < size; i++)
                    {
                        for (int j = 0; j < size; j++)
                        {
                            P[i, j] *= (float) size;
                        }
                    }

                    // Проверяем, что всё в пределах нормы
                    bool error = false;
                    float Max = float.MinValue, Min = float.MaxValue;
                    for (ushort i = 0; i < size; i++)
                    {
                        for (int j = 0; j < size; j++)
                        {
                            if (P[i, j] > 1.001 || P[i, j] < 0.999)
                            {
                                if (!error)
                                    this.task.error.Add(new Error() {Message = $"P[i, j] > 1.001 || P[i, j] < 0.99: P[{i}, {j}] == {P[i, j]}"});

                                error = true;
                                // goto @break;
                            }

                            if (P[i, j] > Max)
                                Max = P[i, j];
                            if (P[i, j] < Min)
                                Min = P[i, j];
                        }
                    }
                    // @break:

                    var sb = new StringBuilder();
                    var ss = 1000f;

                    sb.AppendLine("P martix * " + ss);
                    if (error)
                        sb.AppendLine("P[i, j] > 1.001 || P[i, j] < 0.99");
                    sb.AppendLine($"Min = {Min}, Max = {Max}");

                    for (ushort i = 0; i < size; i++)
                    {
                        for (int j = 0; j < size; j++)
                        {
                            sb.Append(((int)(P[i, j]*ss)).ToString("D3") + "\t");
                        }

                        sb.AppendLine();
                    }

                    File.WriteAllText($"matrix-{task.TableName}-{task.IterationCount.ToString("D2")}.txt", sb.ToString());
                }
            );  // The end of Parallel.foreach sources running
        }

        private void doPermutationTest(float[,] P, float[,] H, ushort[] R, int k, string TableName)
        {
            // doPermutationTest_Keccak   (P, F, H, R);
            for (int i = 0; i < k; i++)
            {
                doPermutationTest_Threefish(P, H, R);
                //DoPermutation(R, tables["base67"]);
                DoPermutation(R, tables[TableName]);
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
            // Работаем с массивом P
            // создать вспомогательный массив и скопировать из него столбец J (столбец с номером указанного байта)
            // при копировании сразу разделить содержимое массива на два, т.к. влияние теперь будет и от других байтов
            // для блока размером blockSize байтов
            //     для каждого байта I из блока
            //         сложить всопогательный массив столбца J со значениями из массива I, но делёными на blockSize и ещё на два

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

        private void doPermutationTest_Keccak(float[,] P, float[,] PH, float[,] H, ushort[] R, int startB)
        {
            
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
                if (tables.Count > 0)
                    return;

                for (int i = 1; i < valueToAdd.Length; i++)
                    GenBaseTable(i);

                GenTransposeTable(128);
                GenTransposeTable(200);
            }
        }

        // 2 и 5 исключены, т.к. являются простыми множителями размера 25600 криптографического состояния
        // Кроме этого, 2 никогда не даёт приращения младшему биту, что криптографически не очень верно
        public static readonly ushort[] valueToAdd = {1, 3, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137};
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
                    throw new Exception($"!newTable.Contains({i}) numberOfTable = {numberOfTable}");
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
                    throw new Exception($"!newTable.Contains({i}) numberOfTable = {0}");
                }
            }

            // В итоге мы получаем таблицу, где для каждого индекса i содержится индекс байта index=newTable[i] в исходной таблице
            // То есть, если у нас есть массивы old и new, то
            // new[i] = old[newTable[i]]
            tables.Add("transpose" + blockSize, newTable);
        }
    }
}
