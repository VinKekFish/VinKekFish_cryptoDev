using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.IO;
using static factorial_library.Factorial;

namespace pc1
{
    class Program
    {
        static void Main(string[] args)
        {
            SortedList<int, (double, BigInteger)> resultDList = new SortedList<int, (double, BigInteger)>(16);

            File.WriteAllText("number.txt", "");
            // File.AppendAllText("number.txt", factorial(721).ToString() + "\r\n");

            Parallel.For
            (
                0, 16,
                delegate(int i)
                {
                    var r = CR(512, i, 16);

                    try
                    {
                        var a = (double) r / 1_000_000.0;

                        lock (resultDList)
                        resultDList.Add(i, (a, r));

                        Console.Write(i.ToString("D2") + " ");
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e.Message);
                    }
                }
            );

            for (int i = 0; i < 16; i++)
            {
                File.AppendAllText("number.txt", i + ": " + resultDList[i].Item1.ToString() + "\t\t" + resultDList[i].Item2.ToString() + "\r\n");
                if (i < resultDList.Count - 1)
                    File.AppendAllText("number.txt", (resultDList[i].Item1 - resultDList[i+1].Item1).ToString() + "\r\n\r\n");
            }
        }

        // Вероятность того, что минимальное количество встреченных чисел будет равна m или выше
        // (n - m*b + b - 1)! / ((n - m*b-1)! * (n + b - 1)!) * (n-1)!
        /// <summary>Величина, 1_000_000 * вероятность того, что минимальное количество встреченных чисел будет равна m или выше</summary>s
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

            var d = (1_000_000 * S2) / S1;

            return d;
        }
    }
}
