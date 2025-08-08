using System;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.Algorithms.SpaceCurves
{
    
    public static class ZCurve
    {
        public static T[] InterleaveSingle<T>(IReadOnlyCollection<T> items, int m)
        {
            return InterleaveSingle(items, items.Count, m);
        }
        
        
        public static T[] InterleaveSingle<T>(IEnumerable<T> items, int size, int m)
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
        
        public static T[] InterleaveMultiple<T>(params IReadOnlyCollection<T>[] itemSequences)
        {
            var itemSize = itemSequences[0].Count;
            if (itemSequences.Any(x=> x.Count != itemSize))
            {
                return InterleaveMultipleDifferentSize(itemSequences);
            }

            var m = itemSequences.Length;

            return InterleaveSingle(itemSequences.SelectMany(x => x), itemSize * m, m);
        }
        
        private static T[] InterleaveMultipleDifferentSize<T>(params IReadOnlyCollection<T>[] itemSequences)
        {
            var (steps, size) = GetChunkSizeAndFinalSize(itemSequences);
            var chunked = itemSequences.Select((x,i)=> x.Chunk(steps[i]));
            var m = itemSequences.Length;
            var interleaved = InterleaveSingle(chunked.SelectMany(x => x), size, m);
            return interleaved.SelectMany(x => x).ToArray();
        }

        private static (int[], int) GetChunkSizeAndFinalSize<T>(IEnumerable<IReadOnlyCollection<T>> itemSequences)
        {
            var steps = itemSequences.Select(x => x.Count).ToArray();
            var gcd = GreatestCommonDivisor(steps);
            for (var i = 0; i < steps.Length; i++)
            {
                steps[i] /= gcd;
            }

            return (steps, gcd * steps.Length);
        }

        private static int GetInterleavedIndex(int idx, int n, int m)
        {
            var i = idx / n;
            var j = idx % n;
            return j * m + i;
        }

        private static int GreatestCommonDivisor(IEnumerable<int> items)
        {
            return items.Aggregate(GreatestCommonDivisor);
        }
        
        private static int GreatestCommonDivisor(int a, int b)
        {
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }

            return a | b;
        }
    }
}