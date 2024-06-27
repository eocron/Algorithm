using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.ProxyHost.Helpers
{
    internal sealed class ConcurrentList<T> : IEnumerable<T> where T : notnull
    {
        private readonly ConcurrentDictionary<T, object> _items = new ConcurrentDictionary<T, object>();

        public void Add(T element)
        {
            _items.TryAdd(element, null);
        }

        public void Remove(T element)
        {
            _items.TryRemove(element, out _);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.Select(x=> x.Key).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}