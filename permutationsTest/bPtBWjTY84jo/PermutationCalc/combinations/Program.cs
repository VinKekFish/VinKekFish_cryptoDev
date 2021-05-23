using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static factorial_library.Factorial;

namespace combinations
{
    class Program
    {
        static void Main(string[] args)
        {
            //var n = 4096;
            //var k = 3072;
            //var n = 16;
            //var k = 4;
            var n = 8;
            var k = 2;
            // Количество сочетаний
            InitFactorialTable(n);

            var variants   = factorial(n) / (factorial(k) * factorial(n - k));

            var countOfBits = 0;
            while (variants >= 2)
            {
                variants /= 2;
                countOfBits++;
            }

            Console.WriteLine(countOfBits + " битов");
            Console.ReadKey();
        }
    }
}
