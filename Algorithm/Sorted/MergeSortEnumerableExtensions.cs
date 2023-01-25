using System;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.Algorithms.Sorted
{
    public static class MergeSortEnumerableExtensions
    {
        public static IEnumerable<TElement> MergeOrderBy<TElement, TKey>(this IEnumerable<TElement> sourceEnumerable, Func<TElement, TKey> keyProvider,
            IEnumerableStorage<TElement> storage, IComparer<TKey> comparer = null, int minimalChunkSize = 1024*1024)
        {
            if(sourceEnumerable == null)
                throw new ArgumentNullException(nameof(sourceEnumerable));
            if (storage == null)
                throw new ArgumentNullException(nameof(storage));
            if (keyProvider == null)
                throw new ArgumentNullException(nameof(keyProvider));

            comparer = comparer ?? Comparer<TKey>.Default;
            storage.Clear();
            foreach (var chunk in sourceEnumerable.ChunkInPlace(minimalChunkSize))
            {
                chunk.Sort((x,y)=> comparer.Compare(keyProvider(x), keyProvider(y)));
                storage.Push(chunk);
            }

            if (storage.Count == 0)
                return Enumerable.Empty<TElement>();
            var queue = new Queue<IEnumerable<TElement>>(storage.Count);
            while (storage.Count > 0)
            {
                queue.Enqueue(storage.Pop());
            }
            while (queue.Count > 1)
            {
                queue.Enqueue(MergeSorted(queue.Dequeue(), queue.Dequeue(), keyProvider, comparer));
            }
            return queue.Dequeue();
        }

        private static IEnumerable<TElement> MergeSorted<TElement, TKey>(IEnumerable<TElement> a, IEnumerable<TElement> b, Func<TElement, TKey> keyProvider, IComparer<TKey> comparer)
        {
            using var aiter = a.GetEnumerator();
            using var biter = b.GetEnumerator();

            var amoved = aiter.MoveNext();
            var bmoved = biter.MoveNext();
            while (amoved || bmoved)
            {
                var cmp = amoved && bmoved ? comparer.Compare(keyProvider(aiter.Current), keyProvider(biter.Current)) : (amoved ? -1 : 1);
                if (cmp <= 0)
                {
                    yield return aiter.Current;
                    amoved = aiter.MoveNext();
                }
                else
                {
                    yield return biter.Current;
                    bmoved = biter.MoveNext();
                }
            }
        }

        private static IEnumerable<List<TValue>> ChunkInPlace<TValue>(
            this IEnumerable<TValue> values,
            int chunkSize)
        {
            var list = new List<TValue>(chunkSize);
            using var enumerator = values.GetEnumerator();
            while (enumerator.MoveNext())
            {
                list.Add(enumerator.Current);
                if (list.Count == chunkSize)
                {
                    yield return list;
                    list.Clear();
                }
            }

            if (list.Count > 0)
                yield return list;
            list.Clear();
        }
    }
}
