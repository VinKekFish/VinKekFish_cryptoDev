using cryptoprime;
using main_tests;
using permutationsTest;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Security.AccessControl;

namespace Test_BytesBuilder
{
/*
 * Тест касается разработки расширенного алгоритма на 4096 битный ключ
 * 
 * Этот тест пытается посмотреть, что можно сделать с Tweak, чтобы дополнительно рандомозировать перестановки
 * Данный тест подсчитывает количество вариантов 32-битного tweak при разных схемах его приращения
 * */
    unsafe class Test_BytesBuilder: MultiThreadTest<Test_BytesBuilder.SourceTask>
    {
        public Test_BytesBuilder(ConcurrentQueue<TestTask> tasks): base(tasks, "Test_BytesBuilder", new Test_BytesBuilder.SourceTaskFabric())
        {
        }

        public new class SourceTask: MultiThreadTest<SourceTask>.SourceTask
        {
            public readonly int  Size;
            public SourceTask(int Size)
            {
                this.Size    = Size;
            }
        }

        public new class SourceTaskFabric: MultiThreadTest<SourceTask>.SourceTaskFabric
        {
            public override IEnumerable<SourceTask> GetIterator()
            {
                yield return new SourceTask(0);
                yield return new SourceTask(1);
                yield return new SourceTask(2);
                yield return new SourceTask(3);
                yield return new SourceTask(4);
                yield return new SourceTask(5);
                yield return new SourceTask(11);
                yield return new SourceTask(4093);
                yield return new SourceTask(4094);
                yield return new SourceTask(4095);
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
                        switch (task.Size)
                        {
                            case 0:  test0(task);
                                     break;
                            case 1:  test0(task);
                                     break;
                            default: 
                                    for (int i = 0; i < 2; i++)
                                    {
                                        testN1(task);
                                        testN2(task);
                                    }
                                    break;
                        }
                    }
                    catch (Exception ex)
                    {
                        var e = new Error() { ex = ex, Message = "Test_BytesBuilder.StartTests: exception " + ex.Message };
                        this.task.error.Add(e);
                    }
                }
            );  // The end of Parallel.foreach sources running

            GC.Collect();
        }

        private void test0(SourceTask task)
        {
            try
            {
                using (new BytesBuilderStatic(task.Size))
                {
                    var e = new Error() { Message = "Test_BytesBuilder.test0: нет исключения" };
                    this.task.error.Add(e);
                }
            }
            catch (ArgumentOutOfRangeException)
            {}
            catch (Exception ex)
            {
                var e = new Error() { ex = ex, Message = "Test_BytesBuilder.test0: exception " + ex.Message };
                this.task.error.Add(e);
            }
        }

        private void testN1(SourceTask task)
        {
            var bbs = new BytesBuilderStatic(task.Size);
            var bbp = new BytesBuilderForPointers();
            var bbb = new BytesBuilder();

            var allocator = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();
            var fix       = new BytesBuilderForPointers.Fixed_AllocatorForUnsafeMemory();
            for (int size = 1; size <= task.Size; size++)
            {
                byte V = 1;
                var  r = allocator.AllocMemory(size);
                while (bbb.Count + size <= size)
                {
                    for (int i = 0; i < r.len; i++)
                        r.array[i] = V++;

                    bbs.add(r);
                    bbp.addWithCopy(r.array, r, allocator);
                    for (int i = 0; i < r.len; i++)
                        bbb.addByte(r.array[i]);
                }
                r.Dispose();

                var res_bbs = bbs.getBytes(r);
                var res_bbp = bbp.getBytes();
                var res_bbb = fix.FixMemory(bbb.getBytes());

                bbs.Clear();
                bbp.Clear();
                bbb.Clear();

                if (!BytesBuilderForPointers.SecureCompare(res_bbs, res_bbp) || !BytesBuilderForPointers.SecureCompare(res_bbs, res_bbb))
                    throw new Exception("Test_BytesBuilder.testN: size = " + task.Size + " unequal");

                res_bbs.Dispose();
                res_bbp.Dispose();
                res_bbb.Dispose();
            }

            bbs.Dispose();
        }

        private void testN2(SourceTask task)
        {return;
            var bbs = new BytesBuilderStatic(task.Size);
            var bbp = new BytesBuilderForPointers();
            var bbb = new BytesBuilder();

            var allocator = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();
            var fix       = new BytesBuilderForPointers.Fixed_AllocatorForUnsafeMemory();
            for (int size = 1; size <= task.Size; size++)
            {
                byte V = 1;
                var  r  = allocator.AllocMemory(size);
                var  r2 = allocator.AllocMemory(size >> 2);
                while (bbb.Count + size <= size)
                {
                    for (int i = 0; i < r.len; i++)
                        r.array[i] = V++;

                    bbs.add(r);
                    bbp.addWithCopy(r.array, r, allocator);
                    for (int i = 0; i < r.len; i++)
                        bbb.addByte(r.array[i]);
                }
                r.Dispose();

                var res_bbs = allocator.AllocMemory(size >> 2);
                bbs.RemoveBytes(size >> 2);
                bbs.getBytesAndRemoveIt(res_bbs, size >> 2);

                var res_bbp = allocator.AllocMemory(size >> 2);
                bbp.getBytesAndRemoveIt(res_bbp);
                bbp.getBytesAndRemoveIt(res_bbp);

                bbb.getBytesAndRemoveIt(resultCount: size >> 2);
                var res_bbb = fix.FixMemory(bbb.getBytesAndRemoveIt(resultCount: size >> 2));

                bbs.Clear();
                bbp.Clear();
                bbb.Clear();

                if (!BytesBuilderForPointers.SecureCompare(res_bbs, res_bbp) || !BytesBuilderForPointers.SecureCompare(res_bbs, res_bbb))
                    throw new Exception("Test_BytesBuilder.testN: size = " + task.Size + " unequal");

                res_bbs.Dispose();
                res_bbp.Dispose();
                res_bbb.Dispose();
            }

            bbs.Dispose();
        }
    }
}
