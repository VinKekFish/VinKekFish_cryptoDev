using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static factorial_library.Factorial;

namespace pc2
{
    class Program
    {
        // Проверка формул для расчёта количества сочетаний с повторениями
        static void Main(string[] args)
        {
            File.WriteAllText("factorial_check.txt", factorial(1023).ToString());

            int n = 20;  // Длина гаммы
            int b = 4;  // Количество вариантов, которые могут быть представлены в одном числе (4 - два бита)
            int m = 1;  // Минимальное число встреченных вариантов каждого числа

            // Расчёт по формуле
            var f1 = factorial(n+b-1)           / factorial(n) / factorial(b-1);
            var f2 = factorial(n - m*b + b - 1) / factorial(n - m*b) / factorial(b - 1);

            Console.WriteLine(f1 + "\t" + f2);
            File.WriteAllText("tmp-f1.txt", f1.ToString());
            File.WriteAllText("tmp-f2.txt", f2.ToString());
            File.WriteAllText("tmp-f2-f1.txt", (1_000_000*f1/f2).ToString());


            int all = 0, allm = 0;
            int[] numbers = new int[b];
            calc(numbers, ref all, ref allm, n, b, m);

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
                if (s < n)
                    return;

                all++;

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
                if (index != b && m > 0)
                    return;

                for (int i = 0; i < index; i++)
                {
                    if (numbers[i] < m)
                        return;
                }

                allm++;
                return;
            }

            for (int i = 0; i <= n - s; i++)
            {
                numbers[index] = i;
                calc(index+1, numbers, ref all, ref allm, n, b, m);
                numbers[index] = 0;
            }
        }
    }
}
