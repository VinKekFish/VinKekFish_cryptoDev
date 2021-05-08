using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

using vinkekfish;
using cryptoprime.VinKekFish;
using main_tests;
using System.IO;
using System.Drawing;

namespace permutationsTest
{
    public unsafe class LightRandomGeneratorTest
    {
        main_tests.TestTask task;
        public LightRandomGeneratorTest(ConcurrentQueue<TestTask> tasks)
        {
            task = new TestTask("LightRandomGenerator_test01", StartTests);
            tasks.Enqueue(task);
        }

        int[] counts;
        public void StartTests()
        {
            const int ModNumber = 4;
            //const int Count     = 512;
            const int Count     = 1024*1024;
            counts = new int[ModNumber];

            for (int i = 0; i < counts.Length; i++)
                counts[i] = 0;

            using (var l = new LightRandomGenerator(Count))
            {
                l.WaitForGenerator();

                var len = l.GeneratedBytes.len;
                var a = l.GeneratedBytes.array;

                lock (this)
                    for (int i = 0; i < len; i++)
                    {
                        var mod = a[i] % ModNumber;
                        counts[mod]++;
                        a[i] = (byte)mod;
                    }

                var sb = new StringBuilder(ModNumber * 256);
                sb.AppendLine($"Right average is {Count / ModNumber}");
                sb.AppendLine();

                for (int i = 0; i < ModNumber; i++)
                    sb.AppendLine(counts[i].ToString("D4"));

                task.error.Add(new Error() { Message = sb.ToString() });

                File.WriteAllText(@"Z:/LightRandomGeneratorTest_wait.txt", sb.ToString() + "\r\n\r\n" + l.GeneratedBytes.ToString(64*1024));
                Save2ToBitmap(Count, len, a, @"Z:\LightRandomGeneratorTest_wait.bmp");
            }

            for (int i = 0; i < counts.Length; i++)
                counts[i] = 0;

            using (var l = new LightRandomGenerator(Count))
            {
                l.doWaitR = false;
                l.doWaitW = false;
                l.WaitForGenerator();
                Thread.Sleep(500);
                l.doWaitR = true;
                l.doWaitW = true;

                var len = l.GeneratedBytes.len;
                var a   = l.GeneratedBytes.array;

                lock (this)
                for (int i = 0; i < len; i++)
                {
                    var mod = a[i] % ModNumber;
                    counts[mod]++;
                    a[i] = (byte) mod;
                }

                var sb = new StringBuilder(ModNumber * 256);
                for (int i = 0; i < ModNumber; i++)
                    sb.AppendLine(counts[i].ToString("D4"));

                task.error.Add(new Error() { Message = sb.ToString() });

                lock (l)
                File.WriteAllText(@"Z:/LightRandomGeneratorTest_nowait.txt", sb.ToString() + "\r\n\r\n" + l.GeneratedBytes.ToString(64*1024));

                Save2ToBitmap(Count, len, a, @"Z:\LightRandomGeneratorTest_nowait.bmp");
            }

            for (int i = 0; i < counts.Length; i++)
                counts[i] = 0;

            if (false)
            using (var gen = new VinKekFish_k1_base_20210419_keyGeneration())
            {
                gen.Init1(VinKekFishBase_etalonK1.NORMAL_ROUNDS, null, 0);
                var key = new byte[] {1};

                fixed (byte * k = key)
                    gen.Init2(k, 1, null);

                gen.EnterToBackgroundCycle(doWaitR: true, doWaitW: false);
                Thread.Sleep(500);
                gen.ExitFromBackgroundCycle();

                Thread.Sleep(500);

                var a = gen.GetNewKey(Count, 512, null, 4);
                for (int i = 0; i < a.len; i++)
                {
                    var mod = a.array[i] % ModNumber;
                    counts[mod]++;
                    a.array[i] = (byte) mod;
                }

                var sb = new StringBuilder(ModNumber * 256);
                for (int i = 0; i < ModNumber; i++)
                    sb.AppendLine(counts[i].ToString("D4"));

                task.error.Add(new Error() { Message = sb.ToString() });

                File.WriteAllText(@"Z:/LightRandomGeneratorTest_vinkekfish.txt", sb.ToString() + "\r\n\r\n" + a.ToString(64*1024));

                Save2ToBitmap(Count, a.len, a, @"Z:\LightRandomGeneratorTest_vinkekfish.bmp");
                a.Dispose();
            }

            for (int i = 0; i < counts.Length; i++)
                counts[i] = 0;

            var sha = new keccak.SHA3(8192);
            {
                sha.prepareGamma(new byte[] {1}, new byte[] {2});

                var ar = sha.getGamma(Count);
                var fAllocator = new cryptoprime.BytesBuilderForPointers.Fixed_AllocatorForUnsafeMemory();
                var a = fAllocator.FixMemory(ar);
                for (int i = 0; i < a.len; i++)
                {
                    var mod = a.array[i] % ModNumber;
                    counts[mod]++;
                    a.array[i] = (byte) mod;
                }

                var sb = new StringBuilder(ModNumber * 256);
                for (int i = 0; i < ModNumber; i++)
                    sb.AppendLine(counts[i].ToString("D4"));

                task.error.Add(new Error() { Message = sb.ToString() });

                File.WriteAllText(@"Z:/LightRandomGeneratorTest_keccak.txt", sb.ToString() + "\r\n\r\n" + a.ToString(64*1024));

                Save2ToBitmap(Count, a.len, a, @"Z:\LightRandomGeneratorTest_keccak.bmp");
                a.Dispose();
            }
        }

        private static void Save2ToBitmap(int Count, long len, byte* a, string FileName)
        {
            int cnt2 = (int)Math.Sqrt(Count) + 1;
            var img = new Bitmap(cnt2, cnt2, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            int x = 0; int y = 0;
            for (int i = 0; i < len; i++, x++)
            {
                if (x >= cnt2)
                {
                    x = 0;
                    y++;
                }

                img.SetPixel(x, y, (a[i] & 1) == 0 ? Color.Black : Color.White);
            }

            img.Save(FileName);
        }
    }
}
