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
            public readonly string   Number;
            public readonly ushort*  table;
            // public readonly ushort*  table_inv;
            public readonly int      size;
            public readonly int      step;
            public readonly int      step2;
            public readonly int      retries;

            public SourceTask(int size, int step, int step2, int retries = 1)
            {
                this.Number  = $"{step}_{size}_{step2}x{retries}";
                this.step    = step;
                this.step2   = step2;
                this.size    = size;
                this.retries = retries;

                table     = GenTransposeTable((ushort) size, (ushort) step,     stepInEndOfBlocks: (ushort) step2, numberOfRetries: retries);
                // table_inv = GenTransposeTable((ushort) size, (ushort) (step*2 * size / 3200), stepInEndOfBlocks: (ushort) (step2*2 * size / 3200), numberOfRetries: retries);
            }
        }

        public new class SourceTaskFabric: MultiThreadTest<SourceTask>.SourceTaskFabric
        {
            public override IEnumerable<SourceTask> GetIterator()
            {
                for (int i = 1; 3200*i < 65536; i++)
                {
                    yield return new SourceTask(3200*i, 200, 1);
                    yield return new SourceTask(3200*i, 128, 1);
                    yield return new SourceTask(3200*i, 200, 8);
                    yield return new SourceTask(3200*i, 200, 8, 2);
                    yield return new SourceTask(3200*i, 400, 16);
                }
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
                    try
                    {
                        var fileName = $"results/table_transpose-{task.Number}.txt";
                    
                        ushort * t   = task.table;
                        // ushort * t_i = task.table_inv;
                        int      len = task.size;

                        var sb = new StringBuilder(len << 2);
                        for (int i = 0; i < len; i++)
                        {
                            sb.Append(t[i].ToString("D4") + " ");
                            if (i % task.step == (task.step - 1))
                                sb.AppendLine();
                        }

                        sb.AppendLine();
                        File.WriteAllText(fileName, sb.ToString());

                        sb.Clear();
                        /*
                        var s1 = new ushort[len];
                        var s2 = new ushort[len];

                        for (ushort i = 0; i < len; i++)
                            s1[i] = i;

                        for (ushort i = 0; i < len; i++)
                            s2[i] = s1[t[i]];

                        for (ushort i = 0; i < len; i++)
                            s1[i] = s2[t_i[i]];


                        for (int i = 0; i < len; i++)
                        {
                            sb.Append(s1[i].ToString("D4") + " ");
                            if (i % task.step == (task.step - 1))
                                sb.AppendLine();
                        }

                        sb.AppendLine();
                        File.WriteAllText($"results/table_transpose-{len}_{task.step}_{task.step2}_inv.txt", sb.ToString());*/
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e.Message + "\r\n" + e.StackTrace);
                    }
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
