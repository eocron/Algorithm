using System;
using System.Collections.Generic;
using System.Linq;
using Eocron.Algorithms.Queues;
using NTests.Core;
using NUnit.Framework;

namespace NTests
{
    [TestFixture]
    public class FibonacciHeapTests : PriorityQueueTestsBase<int, Guid>
    {
        public override IPriorityQueue<int, Guid> CreateNewQueue()
        {
            return new FibonacciHeap<int, Guid>();
        }

        public override IEnumerable<KeyValuePair<int, Guid>> CreateTestCase()
        {
            return Enumerable.Range(0, 10000)
                .Select(x => new KeyValuePair<int, Guid>(Rnd.Next(-100, 100), Guid.NewGuid()));
        }

        public override bool IsStable => false;

        [Test]
        public void SameDequeue()
        {
            var queue = CreateNewQueue();
            queue.Enqueue(new KeyValuePair<int, Guid>(1, Guid.NewGuid()));
            queue.Enqueue(new KeyValuePair<int, Guid>(1, Guid.NewGuid()));

            Assert.AreEqual(2, queue.Count);
            queue.Dequeue();
            Assert.AreEqual(1, queue.Count);
            queue.Dequeue();
            Assert.AreEqual(0, queue.Count);
        }
    }
}
