﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            while (storage.Count / 2 > 0)
            {
                Parallel.For(0, storage.Count / 2, new ParallelOptions(){MaxDegreeOfParallelism = Environment.ProcessorCount * 2}, _ =>
                {
                    var first = storage.Pop();
                    var second = storage.Pop();

                    storage.Push(MergeSorted(first, second, keyProvider, comparer));
                });
            }

            return storage.Count > 0 ? storage.Pop() : Enumerable.Empty<TElement>();
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