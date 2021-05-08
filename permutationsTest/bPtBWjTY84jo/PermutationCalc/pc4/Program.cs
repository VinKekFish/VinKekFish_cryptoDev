using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static factorial_library.Factorial;

namespace pc4
{
    class Program
    {
        static void Main(string[] args)
        {
            // n - длина гаммы
            // k - количество вхождений в гамму некоторого числа
            // b - количество возможных числел в гамме (при побайтовой гамме b = 256)
            int b = 16;
            int n = 512;
            double minP = 1.0/n / 10.0;

            File.WriteAllText("P.txt", "");
            File.WriteAllText("F.txt", "");

            InitFactorialTable(n);

            double summP = 0;
            for (int k = 0; k < n; k++)
            {
                // n!/k!/(n-k)! * 1/b^k*(1-1/b)^-(n-k)
                var K = factorial(n)/factorial(k)/factorial(n-k);
                // Math.Pow(b, -k)
                for (int j = 0; j < k; j++)
                {
                    K = K / b;
                }

                // var P  = ((double) K) * Math.Pow(b, -k) * Math.Pow(1-1.0/b, n-k);
                var P  = ((double) K) * Math.Pow(1-1.0/b, n-k);
                summP += P;

                if (1.0 - summP < minP)
                    break;

                var str = k.ToString("D2") + ":\t" + P.ToString("F12");
                Console.WriteLine(str);
                File.AppendAllText("P.txt", str + "\r\n");
                File.AppendAllText("F.txt", (n*P).ToString("F3") + "\r\n");
            }

            Console.WriteLine();

            Console.WriteLine(summP.ToString("F12"));
            File.AppendAllText("P.txt", summP.ToString("F12") + "\r\n");

            Console.ReadKey();
        }
    }
}
