using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.Algorithms.Sorted
{
    public sealed class InMemoryEnumerableStorage<T> : IEnumerableStorage<T>
    {
        private readonly ConcurrentBag<List<T>> _bag = new ConcurrentBag<List<T>>();
        public void Push(IEnumerable<T> data)
        {
            _bag.Add(data.ToList());
        }

        public IEnumerable<T> Pop()
        {
            if (_bag.TryTake(out var result))
            {
                return result;
            }

            throw new InvalidOperationException("Storage is empty.");
        }

        public int Count => _bag.Count;
        public void Clear()
        {
            _bag.Clear();
        }
    }
}