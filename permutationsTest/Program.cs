using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using vinkekfish;
using main_tests;
using System.Diagnostics;

namespace permutationsTest
{
    // Добавление новых тестов см. в файле Program_AddTasks.cs, метод AddTasks
    // Программа просто консольно запускается и выполняет написанные тесты многопоточно
    // Подсчитывает количество ошибок
    public static class Program
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
            new LightRandomGeneratorTest(tasks);
        }
    }
}
