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

            lock (sync)
            if (factorials == null)
            {
                factorials    = new BigInteger[32768];
                factorials[0] = 1; // 0!
                factorials[1] = 2; // 2!
                for (int i = 2; i < factorials.Length; i++)
                {
                    long s = i << 1;
                    factorials[i] = s * (s - 1) * factorials[i - 1];
                }
            }

            var r = new BigInteger(1);
            for (int i = fact; i > 0; i--)
            {
                if ((i >> 1) < factorials.Length && (i & 1) == 0)
                {
                    return r * factorials[i >> 1];
                }

                r = i * r;
            }

            return r;
        }
    }
}
