using System;
using System.Collections.Generic;
using System.Linq;
using Eocron.Algorithms.Queues;
using Eocron.Algorithms.Randoms;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests.Core
{
    public abstract class PriorityQueueTestsBase<TPriority, TValue>
    {
        protected readonly Random Rnd = new Random();
        protected abstract IPriorityQueue<TPriority, TValue> CreateNewQueue();
        protected abstract IEnumerable<KeyValuePair<TPriority, TValue>> CreateTestCase();
        protected abstract bool IsStable { get; }

        [Test]
        public void EnqueueDequeue()
        {
            var queue = CreateNewQueue();
            var t = CreateTestCase().ToList();

            Assert.Throws<InvalidOperationException>(() => queue.Peek());
            Assert.Throws<InvalidOperationException>(() => queue.Dequeue());

            for (var i = 0; i < 100; i++)
            {
                var testCase = Rnd.Shuffle(t);
                var orderedTestCase = testCase.OrderBy(x => x.Key).ToList();
                foreach (var keyValuePair in orderedTestCase)
                {
                    queue.Enqueue(keyValuePair);
                }

                Assert.AreEqual(orderedTestCase.Count, queue.Count);

                if (IsStable)
                {
                    foreach (var keyValuePair in orderedTestCase)
                    {
                        Assert.AreEqual(keyValuePair, queue.Peek());
                        Assert.AreEqual(keyValuePair, queue.Dequeue());
                    }
                }
                else
                {
                    foreach (var group in orderedTestCase
                        .GroupBy(x => x.Key)
                        .Select(x => new HashSet<KeyValuePair<TPriority, TValue>>(x, new KeyValueComparer())))
                    {

                        for (int j = 0; j < group.Count; j++)
                        {
                            Assert.IsTrue(group.Contains(queue.Peek()));
                            Assert.IsTrue(group.Contains(queue.Dequeue()));
                        }
                    }
                }


                Assert.AreEqual(0, queue.Count);
                Assert.Throws<InvalidOperationException>(() => queue.Peek());
                Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
            }
        }

        [Test]
        public void EnqueueOrUpdate()
        {
            var queue = CreateNewQueue();
            var t = CreateTestCase().ToList();

            Assert.Throws<InvalidOperationException>(() => queue.Peek());
            Assert.Throws<InvalidOperationException>(() => queue.Dequeue());

            for (var i = 0; i < 100; i++)
            {
                var testCase = Rnd.Shuffle(t);
                var distinctOrderedTestCase = testCase
                    .OrderBy(x => x.Key)
                    .GroupBy(x => x.Key)
                    .Select(x => x.Last())
                    .ToList();
                foreach (var keyValuePair in distinctOrderedTestCase)
                {
                    queue.EnqueueOrUpdate(keyValuePair, x => keyValuePair);
                }

                Assert.AreEqual(distinctOrderedTestCase.Count, queue.Count);

                foreach (var keyValuePair in distinctOrderedTestCase)
                {
                    Assert.AreEqual(keyValuePair, queue.Peek());
                    Assert.AreEqual(keyValuePair, queue.Dequeue());
                }

                Assert.AreEqual(0, queue.Count);
                Assert.Throws<InvalidOperationException>(() => queue.Peek());
                Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
            }
        }



        [Test]
        public void Clear()
        {
            var queue = CreateNewQueue();
            var t = CreateTestCase().ToList();

            foreach (var keyValuePair in t)
            {
                queue.Enqueue(keyValuePair);
            }
            queue.Clear();

            Assert.AreEqual(0, queue.Count);
            Assert.Throws<InvalidOperationException>(() => queue.Peek());
            Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
        }

        public class KeyValueComparer : IEqualityComparer<KeyValuePair<TPriority, TValue>>
        {
            public bool Equals(KeyValuePair<TPriority, TValue> x, KeyValuePair<TPriority, TValue> y)
            {
                return Equals(x.Key, y.Key) && Equals(x.Value, y.Value);
            }

            public int GetHashCode(KeyValuePair<TPriority, TValue> obj)
            {
                return System.HashCode.Combine(obj.Key, obj.Value);
            }
        }
    }
}