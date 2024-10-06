using System;
using System.Collections.Generic;
using System.Linq;
using Eocron.Algorithms.Queues;
using Eocron.Algorithms.Tests.Core;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class FibonacciHeapTests : PriorityQueueTestsBase<int, Guid>
    {
        protected override IPriorityQueue<int, Guid> CreateNewQueue()
        {
            return new FibonacciHeap<int, Guid>();
        }

        protected override IEnumerable<KeyValuePair<int, Guid>> CreateTestCase()
        {
            return Enumerable.Range(0, 10000)
                .Select(x => new KeyValuePair<int, Guid>(Rnd.Next(-100, 100), Guid.NewGuid()));
        }

        protected override bool IsStable => false;

        [Test]
        public void SameDequeue()
        {
            var queue = CreateNewQueue();
            queue.Enqueue(new KeyValuePair<int, Guid>(1, Guid.NewGuid()));
            queue.Enqueue(new KeyValuePair<int, Guid>(1, Guid.NewGuid()));

            ClassicAssert.AreEqual(2, queue.Count);
            queue.Dequeue();
            ClassicAssert.AreEqual(1, queue.Count);
            queue.Dequeue();
            ClassicAssert.AreEqual(0, queue.Count);
        }
    }
}