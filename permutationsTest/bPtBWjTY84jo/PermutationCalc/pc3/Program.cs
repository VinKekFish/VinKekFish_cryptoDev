using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pc2
{
    class Program
    {
        // Распределяем 1 и 0 по массиву
        static void Main(string[] args)
        {
            int n = 3;  // Количество единиц для распределения
            int b = 5;  // Длина массива битов

            int all = 0, allm = 0;
            int[] numbers = new int[b];
            calc(numbers, ref all, ref allm, n, b, 0);

            Console.WriteLine(all + "\t" + allm);

            Console.ReadKey();
        }

        private static void calc(int[] numbers, ref int all, ref int allm, int n, int b, int m)
        {
            File.WriteAllText("permutations.txt", "");
            calc(0, numbers, ref all, ref allm, n, b, m);
        }

        private static void calc(int index, int[] numbers, ref int all, ref int allm, int n, int b, int m)
        {
            int s = 0;
            for (int i = 0; i < index; i++)
            {
                s += numbers[i];
            }

            if (index >= b || s >= n)
            {
                all++;

                if (s != n)
                    return;

                var sb = new StringBuilder(b*3);
                for (int i = 0; i < b; i++)
                {
                    if (i < index)
                        sb.Append(numbers[i] + "\t");
                    else
                        sb.Append(0 + "\t");
                }
                File.AppendAllText("permutations.txt", sb.ToString() + "\r\n");

                // Это если последние элементы number равны 0 и мы их не учитываем.
                /*if (index != b && m > 0)
                    return;
                */

                allm++;
                return;
            }

            // for (int i = 0; i <= n - s; i++)
            {
                numbers[index] = 1;
                calc(index+1, numbers, ref all, ref allm, n, b, m);
                numbers[index] = 0;
                calc(index+1, numbers, ref all, ref allm, n, b, m);
            }
        }
    }
}
