using cryptoprime;
using vinkekfish;
using main_tests;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace permutationsTest
{
    unsafe class BitBytBitShuffleTest
    {
        readonly TestTask task;
        readonly string   TableName;
        readonly int      permutationCount;
        public BitBytBitShuffleTest(ConcurrentQueue<TestTask> tasks, string TableName, int permutationCount)
        {
            task = new TestTask("BitBytBitShuffleTest " + TableName + " * " + permutationCount, StartTests);
            tasks.Enqueue(task);

            sources = SourceTask.GetIterator(task);

            this.TableName        = TableName;
            this.permutationCount = permutationCount;
        }

        class SourceTask
        {
            public string Key;
            public byte[] Value;

            public static IEnumerable<SourceTask> GetIterator(TestTask task)
            {
                ulong size = 2048;
                ulong Size = (size << 3);
                ulong All  = Size*Size;


                for (ulong val1 = 0; val1 < Size; val1++)
                for (ulong val2 = 0; val2 < Size; val2++)
                {
                    var b1 = new byte[size];
                    BytesBuilder.ToNull(b1, 0xFFFF_FFFF__FFFF_FFFF);
                    BitToBytes.resetBit(b1, val1);
                    BitToBytes.resetBit(b1, val2);

                    var b2 = new byte[size];
                    BytesBuilder.ToNull(b2);
                    BitToBytes.setBit(b2, val1);
                    BitToBytes.setBit(b2, val2);

                    var done = (float) ((long) (val1*Size + val2) - 4) / All;
                    if (done > 0)
                        task.done = done * 100.0f;

                    yield return new SourceTask() {Key = "Setted bits with val = " + val1 + "/" + val2,      Value = b1};
                    yield return new SourceTask() {Key = "Resetted bits with set bit #" + val1 + "/" + val2, Value = b2};
                }

                yield break;
            }
        }

        readonly IEnumerable<SourceTask> sources = null;

        public unsafe void StartTests()
        {
            GenerateTables();

            var failTestCount  = 0;
            var fail5TestCount = 0;
            Parallel.ForEach
            (
                sources,
                delegate (SourceTask task)
                {
                    var S = BytesBuilder.CloneBytes(task.Value);
                    fixed (byte * s = S)
                    {
                        Permutation(s, S.LongLength);

                        for (int i = 0; i < S.LongLength - 2; i++)
                        {
                            if (/*s[i] == 0 && */s[i] == s[i + 1] && s[i] == s[i + 2]/* && s[i] == s[i + 3] && s[i] == s[i + 4]*/)
                            {
                                Interlocked.Increment(ref failTestCount);

                                if (i + 4 < S.LongLength)
                                if (/*s[i] == 0 && */s[i] == s[i + 1] && s[i] == s[i + 2] && s[i] == s[i + 3] && s[i] == s[i + 4])
                                {
                                    Interlocked.Increment(ref fail5TestCount);
                                    i += 2;
                                }

                                i += 2;
                            }
                        }
                    }
                }
            );

            // Совпадений по три должно быть примерно 
            // Т.к. для любого встреченного n, вероятность того, что следующие два за ним тоже будут n равна 256*256
            // Всего встречено будет (2048-2) байтов на 32768 тестов, то есть (2048-2)*32768//65536=1024
            // Учитывая SkippedBlock всё ещё нужно домножить на 11
            if (failTestCount > 0)
                this.task.error.Add(new Error() {Message = TableName + " test 3: " + failTestCount + " [right 11253], test 5: " + fail5TestCount + "  [right 0]"});
        }

        public unsafe void Permutation(byte * msg, long len)
        {
            var k = new Keccak_20200918();
            using (var state = new Keccak_abstract.KeccakStatesArray(k.State, false))
            {
                byte* cur = msg;
                var blockLen = keccak.S_len2 << 3;
                var buffer   = new byte[2048];
                // var table    = tables[TableName];
                fixed (byte * buff = buffer)
                fixed (ulong * t = tweak)
                {
                    for (int i = 0; i <= permutationCount; i++)
                    {
                        DoKeccakForAllBlocks(msg, len, state, blockLen);
                        DoKeccakForAllBlocks(msg + 100, len - 100, state, blockLen);
                    }

                    /*
                    DoThreefishForAllBlocks(msg, len, t, state, 128);
                    DoPermutation(msg, table, buff);
                    DoThreefishForAllBlocks(msg + 64, len - 128, t, state, 128);
                    DoPermutation(msg, table, buff);
                    DoThreefishForAllBlocks(msg, len, t, state, 128);*/
                }
            }
        }

        private static void DoPermutation(byte* msg, ushort[] table, byte* buff)
        {
            for (int i = 0; i < 2048; i++)
            {
                buff[i] = msg[table[i]];
            }

            BytesBuilder.CopyTo(2048, 2048, buff, msg);
        }
        
        private static unsafe void DoKeccakForAllBlocks(byte* msg, long len, Keccak_abstract.KeccakStatesArray state, int blockLen)
        {
            byte* cur = msg;
            long i;
            long SB = 0;
            for (i = 0; i <= len - blockLen; SB++)
            {
                keccak.Keccackf((ulong *) cur, state.Clong, state.Blong);

                cur += blockLen;
                i += blockLen;
                if (i > len - blockLen && i != len)
                {
                    var size = blockLen - len % blockLen;
                    i   -= size;
                    cur -= size;
                }
            }
        }

        readonly static ulong[] tweak = new ulong[2];
        private static unsafe void DoThreefishForAllBlocks(byte* msg, long len, int blockLen)
        {
            byte* cur = msg;
            for (int i = 0; i <= len - blockLen; i += blockLen)
            {
                var key = cur + 128;
                if (i == len - blockLen)
                    key = msg;

                ushort *tweak = (ushort *) (   key - (blockLen >> 1)   );
                if (tweak < msg)
                    tweak += blockLen;

                CodeGenerated.Cryptoprimes.Threefish_Static_Generated.Threefish1024_step((ulong *) key, (ulong *) tweak, (ulong *) cur);

                cur += blockLen;
            }
        }

        static readonly SortedList<string, ushort[]> tables = new SortedList<string, ushort[]>(16);
        public const int MaxTableNumber = 16;
        public static void GenerateTables()
        {
            lock (tables)
            {
                if (tables.Count > 0)
                    return;

                for (int i = 1; i <= MaxTableNumber; i++)
                    GenBaseTable(i);
            }
        }

        //                     1  2  3  4  5    6   7   8   9  10  11  12 
        static readonly ushort[] valueToAdd = {1, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67};
        private static void GenBaseTable(int numberOfTable)
        {
            var newTable = new ushort[2048];
            var buffer = new ushort[2048];
            for (ushort i = 0; i < newTable.Length; i++)
            {
                newTable[i] = i;
                buffer[i] = i;
            }

            for (int z = 0; z < numberOfTable; z++)
            {
                ushort j = 0, k = 0;
                for (ushort i = 0; i < newTable.Length; i++)
                {
                    buffer[j++] = newTable[k];

                    k += (ushort) (128 + valueToAdd[z]);
                    if (k >= buffer.Length)
                    {
                        k -= (ushort)buffer.Length;
                        // k += 1;
                    }
                }

                fixed (ushort* nt = newTable, buff = buffer)
                {
                    BytesBuilder.CopyTo(buffer.Length << 1, buffer.Length << 1, (byte*)buff, (byte*)nt);
                }
            }

            for (ushort i = 0; i < newTable.Length; i++)
            {
                if (!newTable.Contains(i))
                {
                    throw new Exception();
                }
            }

            tables.Add("base" + numberOfTable, newTable);
        }
    }
}
