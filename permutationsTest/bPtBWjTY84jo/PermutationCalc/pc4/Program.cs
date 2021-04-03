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
            int b = 256;
            int n = 4096;
            double minP = 1e-6;

            File.WriteAllText("P.txt", "");

            double summP = 0;
            for (int k = 0; k < n; k++)
            {
                // n!/k!/(n-k)! * 1/b^k*(1-1/b)^-(n-k)
                var K = factorial(n)/factorial(k)/factorial(n-k);

                var P  = ((double) K) * Math.Pow(b, -k) * Math.Pow(1-1.0/b, n-k);
                summP += P;

                if (1.0 - summP < minP)
                    break;

                var str = k.ToString("D2") + ":\t" + P.ToString("F6");
                Console.WriteLine(str);
                File.AppendAllText("P.txt", str + "\r\n");
            }

            Console.WriteLine();

            Console.WriteLine(summP.ToString("F6"));
            File.AppendAllText("P.txt", summP.ToString("F6") + "\r\n");

            Console.ReadKey();
        }
    }
}
