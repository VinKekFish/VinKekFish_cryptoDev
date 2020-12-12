using cryptoprime;
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
 * Тест касается разработки расширенного алгоритма на 4096 битный ключ
 * 
 * Этот тест пытается посмотреть, что можно сделать с Tweak, чтобы дополнительно рандомозировать перестановки
 * Данный тест подсчитывает количество вариантов 32-битного tweak при разных схемах его приращения
 * */
    unsafe class TweakTest: MultiThreadTest<TweakTest.SourceTask>
    {
        public TweakTest(ConcurrentQueue<TestTask> tasks): base(tasks, "TweakTest", new TweakTest.SourceTaskFabric())
        {
            if (!Directory.Exists("results"))
                Directory.CreateDirectory("results");
        }

        public new class SourceTask: MultiThreadTest<SourceTask>.SourceTask
        {
            public readonly long Number1 = 1;
            public readonly long Number2 = 1;
            public readonly int  Size;
            public SourceTask(long Number1, long Number2, int Size)
            {
                this.Number1 = Number1;
                this.Number2 = Number2;
                this.Size    = Size;
            }
        }

        public new class SourceTaskFabric: MultiThreadTest<SourceTask>.SourceTaskFabric
        {
            public override IEnumerable<SourceTask> GetIterator()
            {
                yield return new SourceTask(0, 1253539379, 4);
                yield return new SourceTask(1, 0x1_01,   2);
                yield return new SourceTask(1, 0x10_01,  2);
                yield return new SourceTask(0x10_01, 1,  2);
                yield return new SourceTask(22079, 12241, 2);
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
                    var fileName = $"results/r-{task.Size.ToString("D1")}-{task.Number1}-{task.Number2}.txt";
                    if (File.Exists(fileName))
                        return;

                    if (task.Size == 2)
                    {
                        var countOfVariants = TestAllVariants2(task.Number1, task.Number2);
                        File.WriteAllText(fileName, $"countOfVariants = {countOfVariants}");
                    }
                    else
                    if (task.Size == 4)
                    {
                        var countOfVariants = TestAllVariants4(task.Number1, task.Number2);
                        File.WriteAllText(fileName, $"countOfVariants = {countOfVariants}");
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
