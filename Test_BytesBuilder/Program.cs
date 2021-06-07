using main_tests;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_BytesBuilder
{
    class Program
    {
        public static readonly string LogFileNameTempl = "tests-$.log";
        public static          string LogFileName      = null;
        static int Main(string[] args)
        {
            main_tests.Program.AddTasksFunc = Program.AddTasks;
            return main_tests.Program.Main(args);
        }

        private static void AddTasks(ConcurrentQueue<TestTask> tasks)
        {
            // new TweakTest(tasks);
            // new PermutationDiffusionTest(tasks);
            // new PermutationDiffusionTestx5(tasks);
            // new GenTablesTest(tasks);
            new Test_BytesBuilder(tasks);
        }
    }
}
