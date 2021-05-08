using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace factorial_library
{
    public static class Factorial
    {
        static Object sync = new object();
        static BigInteger[] factorials = null;



        public static BigInteger factorial(int fact)
        {
            if (fact < 0)
                throw new ArgumentOutOfRangeException();
            InitFactorialTable(8192);

            var r = new BigInteger(1);
            for (int i = fact; i > 0; i--)
            {
                if ((i >> 3) < factorials.Length && (i & 7) == 0)
                {
                    return r * factorials[i >> 3];
                }

                r = i * r;
            }

            return r;
        }

        public static void InitFactorialTable(int Size)
        {
            lock (sync)
            if (factorials == null)
            {
                // factorials    = new BigInteger[32768];
                // factorials    = new BigInteger[16384];
                factorials = new BigInteger[Size];
                // factorials    = new BigInteger[4096];
                factorials[0] = 1; // 0!
                for (int i = 1; i < factorials.Length; i++)
                {
                    long s = i << 3;
                    var sf = new BigInteger(s);
                    for (int j = 0; j < 7; j++)
                    {
                        s--;
                        sf *= s;
                    }

                    factorials[i] = sf * factorials[i - 1];
                }
            }
        }
    }
}
