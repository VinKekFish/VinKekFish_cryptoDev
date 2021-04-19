using cryptoprime;
using static cryptoprime.VinKekFish.VinKekFishBase_etalonK1;
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
using System.Security.AccessControl;

namespace permutationsTest
{
/*
 * Тест касается вывода для ручной проверки таблиц, сгенерированных GenTables эталонного алгоритма \VinKekFish\cryptoprime\VinKekFish\VinKekFishBase_etalonK1.cs
 * 
 * */
    unsafe class GenTablesTest: MultiThreadTest<GenTablesTest.SourceTask>
    {
        public GenTablesTest(ConcurrentQueue<TestTask> tasks): base(tasks, "GenTablesTest", new GenTablesTest.SourceTaskFabric())
        {
            if (!Directory.Exists("results"))
                Directory.CreateDirectory("results");

            GenTables();
        }

        public new class SourceTask: MultiThreadTest<SourceTask>.SourceTask
        {
            public readonly int Number;
            public SourceTask(int Number)
            {
                this.Number = Number;
            }
        }

        public new class SourceTaskFabric: MultiThreadTest<SourceTask>.SourceTaskFabric
        {
            public override IEnumerable<SourceTask> GetIterator()
            {
                yield return new SourceTask(200);
                yield return new SourceTask(128);
            }
        }


        public unsafe override void StartTests()
        {
            var po = new ParallelOptions();
            po.MaxDegreeOfParallelism = Environment.ProcessorCount;
            Parallel.ForEach
            (
                sources, po,
                delegate (SourceTask task)
                {
                    var fileName = $"results/transpose-{task.Number}.txt";
                    
                    ushort[] t = null;
                    switch (task.Number)
                    {
                        case 128:
                            t = transpose128_3200;
                            break;
                        case 200:
                            t = transpose200_3200;
                            break;
                    }

                    var sb = new StringBuilder(t.Length << 2);
                    for (int i = 0; i < t.Length; i++)
                    {
                        sb.Append(t[i].ToString("D4") + " ");
                        if (i % task.Number == (task.Number - 1))
                            sb.AppendLine();
                    }

                    sb.AppendLine();
                    File.WriteAllText(fileName, sb.ToString());

                    sb.Clear();
                    if (task.Number != 200)
                        return;


                    var s1 = new ushort[t.Length];
                    var s2 = new ushort[t.Length];

                    for (ushort i = 0; i < t.Length; i++)
                        s1[i] = i;

                    for (ushort i = 0; i < t.Length; i++)
                        s2[i] = s1[transpose200_3200[i]];

                    for (ushort i = 0; i < t.Length; i++)
                        s1[i] = s2[transpose200_3200[i]];


                    for (int i = 0; i < t.Length; i++)
                    {
                        sb.Append(s1[i].ToString("D4") + " ");
                        if (i % task.Number == (task.Number - 1))
                            sb.AppendLine();
                    }

                    sb.AppendLine();
                    File.WriteAllText("results/transpose-200-200.txt", sb.ToString());
                }
            );  // The end of Parallel.foreach sources running
        }

        private object TestAllVariants4(long number1, long number2)
        {
            ulong  r  = 0;
            uint   n1 = (uint) number1;
            uint   n2 = (uint) number2;

            var dict = new byte[0x1_0000_0000 >> 3];

            uint num = 0;
            while (true)
            {
                num += n1;
                num += n2;

                if (cryptoprime.BitToBytes.getBit(dict, num))
                    break;

                cryptoprime.BitToBytes.setBit(dict, num);
                r++;
            }

            return r;
        }

        private ulong TestAllVariants2(long number1, long number2)
        {
            ulong  r  = 0;
            ushort n1 = (ushort) number1;
            ushort n2 = (ushort) number2;

            var dict = new byte[0x1_0000_0000 >> 3];

            uint num = 0;
            while (true)
            {
                num += (uint) (n2 << 16);
                num += n1;

                if (cryptoprime.BitToBytes.getBit(dict, num))
                    break;

                cryptoprime.BitToBytes.setBit(dict, num);
                r++;
            }

            return r;
        }
    }
}
