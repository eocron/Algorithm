using System;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.Algorithms.SpaceCurves
{
    public static class ZCurve
    {
        public static T[] Interleave<T>(IReadOnlyCollection<T> items, int m)
        {
            return Interleave<T>(items, items.Count, m);
        }
        
        
        public static T[] Interleave<T>(IEnumerable<T> items, int size, int m)
        {
            if (size % m != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(m), "Array must be a multiple of " + m);
            }
            var output = new T[size];
            var n = size / m;
            var i = 0;
            foreach (var item in items)
            {
                output[GetInterleavedIndex(i, n, m)] = item;
                i++;
            }
            return output;
        }
        
        public static T[] Interleave<T>(params IReadOnlyCollection<T>[] itemSequences)
        {
            var itemSize = itemSequences[0].Count;
            if (itemSequences.Any(x=> x.Count != itemSize))
            {
                throw new ArgumentOutOfRangeException(nameof(itemSequences), "Sequences should have same size");
            }

            var m = itemSequences.Length;

            return Interleave(itemSequences.SelectMany(x => x), itemSize * m, m);
        }
        
        public static int GetInterleavedIndex(int idx, int n, int m)
        {
            var i = idx / n;
            var j = idx % n;
            return j * m + i;
        }
    }
}