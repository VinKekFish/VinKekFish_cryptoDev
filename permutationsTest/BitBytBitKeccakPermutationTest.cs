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
    unsafe class BitBytBitKeccakPermutationTest
    {
        readonly TestTask task;
        readonly string   TableName;
        readonly int      permutationCount;
        public BitBytBitKeccakPermutationTest(ConcurrentQueue<TestTask> tasks, string TableName, int permutationCount)
        {
            task = new TestTask("BitBytBitKeccakPermutationTest " + TableName + " * " + permutationCount, StartTests);
            tasks.Enqueue(task);

            sources = SourceTask.GetIterator();

            this.TableName        = TableName;
            this.permutationCount = permutationCount;
        }

        class SourceTask
        {
            public string Key;
            public byte[] Value;
            public int    SkippedBlock;

            public static IEnumerable<SourceTask> GetIterator()
            {
                ulong size = 2048;

                for (int j = 0; j <= 10; j++)
                for (ulong val = 0; val < (ulong) (size << 3); val++)
                {
                    var b1 = new byte[size];
                    BytesBuilder.ToNull(b1, 0xFFFF_FFFF__FFFF_FFFF);
                    BitToBytes.resetBit(b1, val);

                    var b2 = new byte[size];
                    BytesBuilder.ToNull(b2);
                    BitToBytes.setBit(b2, val);

                    yield return new SourceTask() {Key = "Setted bits with val = " + val,      Value = b1, SkippedBlock = j};
                    yield return new SourceTask() {Key = "Resetted bits with set bit #" + val, Value = b2, SkippedBlock = j};
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
                        Permutation(s, S.LongLength, task.SkippedBlock);

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

        public unsafe void Permutation(byte * msg, long len, long skippedBlock)
        {
            var k = new Keccak_20200918();
            using (var state = new Keccak_abstract.KeccakStatesArray(k.State, false))
            {
                byte* cur = msg;
                var blockLen = keccak.S_len2 << 3;
                var buffer   = new byte[2048];
                var table    = tables[TableName];
                fixed (byte * buff = buffer)
                fixed (ulong * t = tweak)
                {
                    for (int i = 0; i <= permutationCount; i++)
                    {
                        DoKeccakForAllBlocks(msg, len, state, blockLen, skippedBlock);
                        DoPermutation(msg, table, buff);
                        DoKeccakForAllBlocks(msg, len, state, blockLen, skippedBlock);
                        DoPermutation(msg, tables["base1"], buff);
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
        
        private static unsafe void DoKeccakForAllBlocks(byte* msg, long len, Keccak_abstract.KeccakStatesArray state, int blockLen, long skippedBlock)
        {
            byte* cur = msg;
            long i;
            long SB = 0;
            for (i = 0; i <= len - blockLen; SB++)
            {
                var cr = cur;
                var fl = false;
                for (int j = 0; j < blockLen; j++)
                {
                    // Специально для тестирования пропускаем блоки, где нет данных
                    if (cr[j] != 0 && cr[j] != 255)
                    {
                        fl = true;
                        break;
                    }
                }

                if (fl && SB != skippedBlock)
                    keccak.Keccackf((ulong *) cur, state.Clong, state.Blong);

                cur += blockLen;
                i += blockLen;
                if (i > len - blockLen && i != len && SB != skippedBlock)
                {
                    var size = blockLen - len % blockLen;
                    i   -= size;
                    cur -= size;
                }
            }
        }

        readonly static ulong[] tweak = new ulong[2];
        private static unsafe void DoThreefishForAllBlocks(byte* msg, long len, ulong * tweak, Keccak_abstract.KeccakStatesArray state, int blockLen)
        {
            byte* cur = msg;
            for (int i = 0; i <= len - blockLen; i += blockLen)
            {
                var text = cur + 128;
                if (i == len - blockLen)
                    text = msg;

                var cr = cur;
                var fl = false;
                for (int j = 0; j < blockLen; j++)
                {
                    // Специально для тестирования пропускаем блоки, где нет данных
                    if (text[j] != 0 && text[j] != 255)
                    {
                        fl = true;
                        break;
                    }
                }

                if (fl)
                {
                    CodeGenerated.Cryptoprimes.Threefish_Static_Generated.Threefish1024_step((ulong *) cur, (ulong *) tweak, (ulong *) text);
                }

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
