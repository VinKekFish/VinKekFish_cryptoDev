using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.IO;
using static factorial_library.Factorial;
using System.Security.Cryptography;
using cryptoprime;

namespace pc1
{
    class Program
    {
        static object sync = new object();
        static void Main(string[] args)
        {
            try
            {
                Process();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                lock (sync)
                File.AppendAllText("error.log", DateTime.Now.ToString() + ":\r\n" + e.Message + "\r\n" + e.StackTrace + "\r\n\r\n\r\n");
            }
        }

        private static void Process()
        {
            File.WriteAllText("number.txt", "");
            // File.AppendAllText("number.txt", factorial(721).ToString() + "\r\n");

            var b = 256;
            // var n = 65536;
            var n = 4096;
            var N = n / b;

            SortedList<int, (double, BigInteger)> resultDList = new SortedList<int, (double, BigInteger)>(N);

            var po = new ParallelOptions();
            po.MaxDegreeOfParallelism = Environment.ProcessorCount;
            Parallel.For
            (
                0, N, po,
                delegate (int i)
                {
                    try
                    {
                        var r = CR(n, i, b);

                        var a = 1_000_000.0 / (double) r;

                        lock (resultDList)
                            resultDList.Add(i, (a, r));

                        Console.Write(i.ToString("D2") + " ");
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e.Message);
                        lock (sync)
                        File.AppendAllText("error.log", DateTime.Now.ToString() + ":\r\n" + e.Message + $"\r\nn={n}, b={b}, m={i}" + "\r\n" + e.StackTrace + "\r\n\r\n\r\n");
                    }
                }
            );

            for (int i = 0; i < N; i++)
            {
                File.AppendAllText("number.txt", i + ": " + resultDList[i].Item1.ToString()/* + "\t\t" + resultDList[i].Item2.ToString()*/ + "\r\n");
                if (i < resultDList.Count - 1)
                    File.AppendAllText("number.txt", (resultDList[i].Item1 - resultDList[i + 1].Item1).ToString() + "\r\n\r\n");
            }

            // Проверяем вычисленную статистику
            if (b == 256)
            {
                List<byte[]> list = new List<byte[]>(N);
                long someNumber = 0;
                var sh = new System.Security.Cryptography.SHA512Managed();
                for (int i = 0; i < N; i++)
                {
                    var bb = new BytesBuilder();
                    do
                    {
                        someNumber++;
                        var ba = new ASCIIEncoding().GetBytes(someNumber.ToString());
                        bb.add(  sh.ComputeHash(ba)  );
                    }
                    while (bb.Count < n);

                    list.Add(  bb.getBytes(n)  );

                    bb.clear();
                    bb = null;
                }

                int summ = 0;
                for (int i = 0; i < N; i++)
                {
                    var ba = list[i];
                    for (int j = 0; j < 256; j++)
                        if (  ! ba.Contains<byte>((byte) j)   )
                        {
                            summ++;
                            break;
                        }
                }

                File.AppendAllText("number.txt", "summ / N: " + (summ / N).ToString("F6") + "\t" + summ + "\t" + N + $"\r\nn={n}, b={b}" + "\r\n");
            }
        }

        // Вероятность того, что минимальное количество встреченных чисел будет равна m или выше
        // (n - m*b + b - 1)! / ((n - m*b-1)! * (n + b - 1)!) * (n-1)!
        /// <summary>Величина, умноженная на 1_000_000 и делить вероятность того, что минимальное количество встреченных чисел будет равна m или выше</summary>s
        /// <param name="n">n - длина гаммы</param>
        /// <param name="m">Минмальное количество встреченных чисел какого-то одного значения</param>
        /// <param name="b">Количество вариантов чисел</param>
        /// <returns>Вероятность</returns>
        static BigInteger CR(int n, int m, int b)
        {
            // S1 - Общее количество возможных сочетаний с повторениями
            // S2 - Количество сочетаний с повторениями не менее по m вхождений
            var S1 = factorial(n+b-1) / factorial(n) / factorial(b-1);
            var S2 = factorial(n - m*b + b - 1) / factorial(n - m*b) / factorial(b - 1);

            var d = (1_000_000 * S1) / S2;

            return d;
        }
    }
}
