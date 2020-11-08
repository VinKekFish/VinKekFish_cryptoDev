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
    unsafe class BitBytBitThreeFishPermutationTest
    {
        readonly TestTask task;
        public BitBytBitThreeFishPermutationTest(ConcurrentQueue<TestTask> tasks)
        {
            task = new TestTask("BitBytBitThreeFishPermutationTest", StartTests);
            tasks.Enqueue(task);

            sources = SourceTask.GetIterator();
        }

        class SourceTask
        {
            public string Key;
            public byte[] Value;

            public static IEnumerable<SourceTask> GetIterator()
            {
                ulong size = 2048;

                for (ulong val = 0; val < (ulong) (size << 3); val++)
                {
                    var b1 = new byte[size];
                    BytesBuilder.ToNull(b1, 0xFFFF_FFFF__FFFF_FFFF);
                    BitToBytes.resetBit(b1, val);

                    var b2 = new byte[size];
                    BytesBuilder.ToNull(b2);
                    BitToBytes.setBit(b2, val);

                    yield return new SourceTask() {Key = "Setted bits with val = " + val, Value = b1};
                    yield return new SourceTask() {Key = "Resetted bits with set bit #" + val, Value = b2};
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

                                    break;
                                }

                                break;
                            }
                        }
                    }
                }
            );

            // Совпадений по три должно быть примерно 
            // Т.к. для любого встреченного n, вероятность того, что следующие два за ним тоже будут n равна 256*256
            // Всего встречено будет (2048-2) байтов на 32768 тестов, то есть (2048-2)*32768//65536=1024
            if (failTestCount > 0)
                this.task.error.Add(new Error() {Message = "Fail test " + failTestCount});
        }

        public unsafe void Permutation(byte * msg, long len)
        {
            var k = new Keccak_20200918();
            using (var state = new Keccak_abstract.KeccakStatesArray(k.State, false))
            {
                byte* cur = msg;
                var table    = tables["base8"];
                fixed (ulong *  t = tweak)
                fixed (ushort * T = table)
                {
                    DoThreefishForAllBlocks(msg, len, t, state, 128, T, table.Length);
                }
            }
        }

        readonly static ulong[] tweak = new ulong[2];
        private static unsafe void DoThreefishForAllBlocks(byte* msg, long len, ulong * tweak, Keccak_abstract.KeccakStatesArray state, int blockLen, ushort * table, int tableLen)
        {
            byte* cur = msg;
            for (int i = 0; i <= len - blockLen; i += blockLen)
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

                if (fl)
                {
                    var text = cur + 128;
                    if (i == len - blockLen)
                        text = msg;

                    CodeGenerated.Cryptoprimes.Threefish_Static_Generated.Threefish1024_step((ulong *) cur, (ulong *) tweak, (ulong *) text);
                }

                cur += blockLen;
            }
        }

        readonly SortedList<string, ushort[]> tables = new SortedList<string, ushort[]>(16);
        private void GenerateTables()
        {
            if (tables.Count > 0)
                return;

            var newTable = new ushort[2048*3];
            // var buffer   = new ushort[2048*3];
            for (ushort i = 0; i < newTable.Length; i++)
            {
                newTable[i] = i;
                // buffer  [i] = i;
            }

            for (ushort i = 0; i < newTable.Length; i++)
            {
                if (!newTable.Contains(i))
                {
                    throw new Exception();
                }
            }

            tables.Add("base8", newTable);
        }
    }
}
