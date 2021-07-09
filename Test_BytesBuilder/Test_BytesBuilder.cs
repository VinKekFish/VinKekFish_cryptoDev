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
                yield return new SourceTask(4096);
                yield return new SourceTask(4097);
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
                                        testN3(task);
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
                var t8 = new byte[8];
                ulong data = 0xA5A5_B5A5_A5A5_B5B5;
                BytesBuilder.ULongToBytes(data, ref t8);
                BytesBuilder.BytesToULong(out ulong dt, t8, 0);

                if (dt != data)
                {
                    var e = new Error() { Message = "Test_BytesBuilder.test0: ULongToBytes != BytesToULong" };
                    this.task.error.Add(e);
                }

                BytesBuilder.BytesToUInt(out uint dt4, t8, 0);
                if (dt4 != (uint) data)
                {
                    var e = new Error() { Message = "Test_BytesBuilder.test0: ULongToBytes/4 != BytesToUInt" };
                    this.task.error.Add(e);
                }

                BytesBuilder.BytesToUInt(out dt4, t8, 4);
                if (dt4 != (uint) (data >> 32))
                {
                    var e = new Error() { Message = "Test_BytesBuilder.test0: ULongToBytes>>/4 != BytesToUInt" };
                    this.task.error.Add(e);
                }

                var fa = new BytesBuilderForPointers.Fixed_AllocatorForUnsafeMemory();
                var b1 = new byte[8];
                var r1 = fa.FixMemory(b1);
                var r2 = fa.FixMemory(t8);
                if (BytesBuilderForPointers.isArrayEqual_Secure(r1, r2))
                {
                    var e = new Error() { Message = "Test_BytesBuilder.test0: isArrayEqual_Secure(r1, r2)" };
                    this.task.error.Add(e);
                }
                BytesBuilder.CopyTo(t8, b1, 0, 7);
                if (BytesBuilderForPointers.isArrayEqual_Secure(r1, r2))
                {
                    var e = new Error() { Message = "Test_BytesBuilder.test0: isArrayEqual_Secure(r1, r2) [2]" };
                    this.task.error.Add(e);
                }

                b1[7] = t8[7];
                if (!BytesBuilderForPointers.isArrayEqual_Secure(r1, r2))
                {
                    var e = new Error() { Message = "Test_BytesBuilder.test0: isArrayEqual_Secure(r1, r2) [3]" };
                    this.task.error.Add(e);
                }

                r1.Clear();
                BytesBuilderForPointers.BytesToULong(out dt, r2, 0, 8);

                if (dt != data)
                {
                    var e = new Error() { Message = "Test_BytesBuilder.test0: ULongToBytes != BytesToULong (BytesBuilderForPointers)" };
                    this.task.error.Add(e);
                }

                r1.Dispose();
                r2.Dispose();

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
            using (var bbs = new BytesBuilderStatic(task.Size))
            {
                var bbp = new BytesBuilderForPointers();
                var bbb = new BytesBuilder();

                var allocator = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();
                var fix       = new BytesBuilderForPointers.Fixed_AllocatorForUnsafeMemory();
                for (int size = 1; size <= task.Size; size++)
                {
                    BytesBuilderForPointers.Record res_bbs = null, res_bbb = null, res_bbp = null, res_bbs2 = null, r = null;

                    try
                    {
                        try
                        {
                            byte V = 1;
                            r = allocator.AllocMemory(size);
                            while (bbb.Count + size <= size)
                            {
                                for (int i = 0; i < r.len; i++)
                                    r.array[i] = V++;

                                bbs.add(r);
                                bbp.addWithCopy(r, r.len, allocator);
                                for (int i = 0; i < r.len; i++)
                                    bbb.addByte(r.array[i]);
                            }
                            r.Dispose();

                            res_bbs = bbs.getBytes();
                            res_bbp = bbp.getBytes();
                            res_bbb = fix.FixMemory(bbb.getBytes());

                            bbs.Resize(bbs.size + 1);
                            res_bbs2 = bbs.getBytes();
                        }
                        finally
                        {
                            bbs.Clear();
                            bbp.Clear();
                            bbb.Clear();
                        }

                        if (!BytesBuilderForPointers.isArrayEqual_Secure(res_bbs, res_bbp) || !BytesBuilderForPointers.isArrayEqual_Secure(res_bbs, res_bbb) || !BytesBuilderForPointers.isArrayEqual_Secure(res_bbs2, res_bbb))
                            throw new Exception("Test_BytesBuilder.testN: size = " + task.Size + " unequal");
                    }
                    finally
                    {
                        res_bbs ?.Dispose();
                        res_bbp ?.Dispose();
                        res_bbb ?.Dispose();
                        res_bbs2?.Dispose();
                    }
                }
            }
        }

        private void testN2(SourceTask task)
        {
            using (var bbs = new BytesBuilderStatic(task.Size))
            {
                var bbp = new BytesBuilderForPointers();
                var bbb = new BytesBuilder();

                var allocator = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();
                var fix       = new BytesBuilderForPointers.Fixed_AllocatorForUnsafeMemory();
                for (int size = 2; size <= task.Size; size++)
                {
                    BytesBuilderForPointers.Record res_bbs = null, res_bbp = null, res_bbb = null;
                    try
                    {
                        try
                        {
                            byte V = 1;
                            var  r  = allocator.AllocMemory(size);
                            // var  r2 = allocator.AllocMemory(size >> 1);
                            while (bbb.Count + size <= size)
                            {
                                for (int i = 0; i < r.len; i++)
                                    r.array[i] = V++;

                                bbs.add(r);
                                bbp.addWithCopy(r, r.len, allocator);
                                for (int i = 0; i < r.len; i++)
                                    bbb.addByte(r.array[i]);
                            }
                            r.Dispose();
                            r = null;

                            var remainder = size - ((size >> 1) << 1);

                            res_bbs = allocator.AllocMemory(size >> 1);
                            bbs.RemoveBytes(size >> 1);
                            bbs.getBytesAndRemoveIt(res_bbs, size >> 1);
                            if (bbs.Count != remainder)
                                throw new Exception("Test_BytesBuilder.testN: size = " + task.Size + ", bbs.Count > 1: " + bbs.Count);

                            res_bbp = allocator.AllocMemory(size >> 1);
                            bbp.getBytesAndRemoveIt(res_bbp);
                            bbp.getBytesAndRemoveIt(res_bbp);
                            if (bbp.Count != remainder)
                                throw new Exception("Test_BytesBuilder.testN: size = " + task.Size + ", bbp.Count > 1: " + bbp.Count);

                            bbb.getBytesAndRemoveIt(resultCount: size >> 1);
                            res_bbb = fix.FixMemory(bbb.getBytesAndRemoveIt(resultCount: size >> 1));
                            if (bbb.Count != remainder)
                                throw new Exception("Test_BytesBuilder.testN: size = " + task.Size + ", bbb.Count > 1: " + bbb.Count);
                        }
                        finally
                        {
                            bbs.Clear();
                            bbp.Clear();
                            bbb.Clear();
                        }

                        if (!BytesBuilderForPointers.isArrayEqual_Secure(res_bbs, res_bbp) || !BytesBuilderForPointers.isArrayEqual_Secure(res_bbs, res_bbb))
                            throw new Exception("Test_BytesBuilder.testN: size = " + task.Size + " unequal");

                    }
                    finally
                    {
                        res_bbs?.Dispose();
                        res_bbp?.Dispose();
                        res_bbb?.Dispose();
                    }
                }
            }
        }

        private void testN3(SourceTask task)
        {
            for (int cst = 1; cst < 11 && cst < task.Size; cst++)
            using (var bbs = new BytesBuilderStatic(task.Size))
            {
                var bbp = new BytesBuilderForPointers();
                var bbb = new BytesBuilder();

                var allocator = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();
                var fix       = new BytesBuilderForPointers.Fixed_AllocatorForUnsafeMemory();
                int size = task.Size;
                {
                    BytesBuilderForPointers.Record res_bbs = null, res_bbp = null, res_bbb = null;
                    try
                    {
                        try
                        {
                            byte V = 1;
                            var  r  = allocator.AllocMemory(size - 1);
                            // var  r2 = allocator.AllocMemory(size >> 1);
                            while (bbb.Count < size - 1)
                            {
                                for (int i = 0; i < r.len; i++)
                                    r.array[i] = V++;

                                bbs.add(r);
                                bbp.addWithCopy(r, r.len, allocator);
                                for (int i = 0; i < r.len; i++)
                                    bbb.addByte(r.array[i]);
                            }
                            bbs.add(r.array, 1);
                            bbp.addWithCopy(r.array, 1, allocator);
                            bbb.addByte(r.array[0]);

                            r.Dispose();
                            r = null;

                            int remainder = size;
                            do
                            {
                                remainder -= cst;

                                try
                                {
                                    res_bbs = allocator.AllocMemory(cst);
                                    bbs.getBytesAndRemoveIt(res_bbs);
                                    if (bbs.Count != remainder)
                                        throw new Exception("Test_BytesBuilder.testN: size = " + task.Size + ", bbs.Count > 1: " + bbs.Count);

                                    res_bbp = allocator.AllocMemory(cst);
                                    bbp.getBytesAndRemoveIt(res_bbp);
                                    if (bbp.Count != remainder)
                                        throw new Exception("Test_BytesBuilder.testN: size = " + task.Size + ", bbp.Count > 1: " + bbp.Count);

                                    res_bbb = fix.FixMemory(bbb.getBytesAndRemoveIt(resultCount: cst));
                                    if (bbb.Count != remainder)
                                        throw new Exception("Test_BytesBuilder.testN: size = " + task.Size + ", bbb.Count > 1: " + bbb.Count);

                                    if (!BytesBuilderForPointers.isArrayEqual_Secure(res_bbs, res_bbp) || !BytesBuilderForPointers.isArrayEqual_Secure(res_bbs, res_bbb))
                                        throw new Exception("Test_BytesBuilder.testN: size = " + task.Size + " unequal");
                                }
                                finally
                                {
                                    res_bbs?.Dispose();
                                    res_bbp?.Dispose();
                                    res_bbb?.Dispose();
                                    res_bbs = null;
                                    res_bbp = null;
                                    res_bbb = null;
                                }

                                try
                                {
                                    res_bbs = bbs.getBytes();
                                    res_bbp = bbp.getBytes();
                                    res_bbb = fix.FixMemory(bbb.getBytes());

                                    if (!BytesBuilderForPointers.isArrayEqual_Secure(res_bbs, res_bbp) || !BytesBuilderForPointers.isArrayEqual_Secure(res_bbs, res_bbb))
                                            throw new Exception("Test_BytesBuilder.testN: size = " + task.Size + " unequal [2]");
                                }
                                finally
                                {
                                    res_bbs?.Dispose();
                                    res_bbp?.Dispose();
                                    res_bbb?.Dispose();
                                    res_bbs = null;
                                    res_bbp = null;
                                    res_bbb = null;
                                }
                            }
                            while (bbs.Count > cst);
                        }
                        finally
                        {
                            bbs.Clear();
                            bbp.Clear();
                            bbb.Clear();
                        }
                    }
                    finally
                    {
                        res_bbs?.Dispose();
                        res_bbp?.Dispose();
                        res_bbb?.Dispose();
                        res_bbs = null;
                        res_bbp = null;
                        res_bbb = null;
                    }
                }
            }
        }
    }
}
