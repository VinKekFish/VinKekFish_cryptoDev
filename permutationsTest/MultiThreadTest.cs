using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using main_tests;

namespace permutationsTest
{
    public abstract class MultiThreadTest<T> where T: MultiThreadTest<T>.SourceTask
    {
        protected readonly TestTask task;
        public MultiThreadTest(ConcurrentQueue<TestTask> tasks, string name, SourceTaskFabric fabric)
        {
            task = new TestTask(name, StartTests);
            tasks?.Enqueue(task);

            sources = fabric.GetIterator();
        }

        public abstract void StartTests();

        protected readonly IEnumerable<T> sources = null;

        public class SourceTask
        {
        }

        public abstract class SourceTaskFabric
        {
            public abstract IEnumerable<T> GetIterator();
        }
    }
}
