using System;
using System.Collections.Generic;
using System.Linq;
using Eocron.Algorithms.Queues;
using Eocron.Algorithms.Randoms;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Eocron.Algorithms.Tests.Core
{
    public abstract class PriorityQueueTestsBase<TPriority, TValue>
    {
        [Test]
        public void Clear()
        {
            var queue = CreateNewQueue();
            var t = CreateTestCase().ToList();

            foreach (var keyValuePair in t) queue.Enqueue(keyValuePair);
            queue.Clear();

            ClassicAssert.AreEqual(0, queue.Count);
            ClassicAssert.Throws<InvalidOperationException>(() => queue.Peek());
            ClassicAssert.Throws<InvalidOperationException>(() => queue.Dequeue());
        }

        [Test]
        public void EnqueueDequeue()
        {
            var queue = CreateNewQueue();
            var t = CreateTestCase().ToList();

            ClassicAssert.Throws<InvalidOperationException>(() => queue.Peek());
            ClassicAssert.Throws<InvalidOperationException>(() => queue.Dequeue());

            for (var i = 0; i < 100; i++)
            {
                var testCase = Rnd.Shuffle(t);
                var orderedTestCase = testCase.OrderBy(x => x.Key).ToList();
                foreach (var keyValuePair in orderedTestCase) queue.Enqueue(keyValuePair);

                ClassicAssert.AreEqual(orderedTestCase.Count, queue.Count);

                if (IsStable)
                    foreach (var keyValuePair in orderedTestCase)
                    {
                        ClassicAssert.AreEqual(keyValuePair, queue.Peek());
                        ClassicAssert.AreEqual(keyValuePair, queue.Dequeue());
                    }
                else
                    foreach (var group in orderedTestCase
                                 .GroupBy(x => x.Key)
                                 .Select(x => new HashSet<KeyValuePair<TPriority, TValue>>(x, new KeyValueComparer())))
                        for (var j = 0; j < group.Count; j++)
                        {
                            ClassicAssert.IsTrue(group.Contains(queue.Peek()));
                            ClassicAssert.IsTrue(group.Contains(queue.Dequeue()));
                        }


                ClassicAssert.AreEqual(0, queue.Count);
                ClassicAssert.Throws<InvalidOperationException>(() => queue.Peek());
                ClassicAssert.Throws<InvalidOperationException>(() => queue.Dequeue());
            }
        }

        [Test]
        public void EnqueueOrUpdate()
        {
            var queue = CreateNewQueue();
            var t = CreateTestCase().ToList();

            ClassicAssert.Throws<InvalidOperationException>(() => queue.Peek());
            ClassicAssert.Throws<InvalidOperationException>(() => queue.Dequeue());

            for (var i = 0; i < 100; i++)
            {
                var testCase = Rnd.Shuffle(t);
                var distinctOrderedTestCase = testCase
                    .OrderBy(x => x.Key)
                    .GroupBy(x => x.Key)
                    .Select(x => x.Last())
                    .ToList();
                foreach (var keyValuePair in distinctOrderedTestCase)
                    queue.EnqueueOrUpdate(keyValuePair, x => keyValuePair);

                ClassicAssert.AreEqual(distinctOrderedTestCase.Count, queue.Count);

                foreach (var keyValuePair in distinctOrderedTestCase)
                {
                    ClassicAssert.AreEqual(keyValuePair, queue.Peek());
                    ClassicAssert.AreEqual(keyValuePair, queue.Dequeue());
                }

                ClassicAssert.AreEqual(0, queue.Count);
                ClassicAssert.Throws<InvalidOperationException>(() => queue.Peek());
                ClassicAssert.Throws<InvalidOperationException>(() => queue.Dequeue());
            }
        }

        protected abstract IPriorityQueue<TPriority, TValue> CreateNewQueue();
        protected abstract IEnumerable<KeyValuePair<TPriority, TValue>> CreateTestCase();
        protected abstract bool IsStable { get; }
        protected readonly Random Rnd = new();

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