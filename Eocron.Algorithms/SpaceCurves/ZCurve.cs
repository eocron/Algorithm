using System;
using System.Collections.Generic;

namespace Eocron.Algorithms.SpaceCurves
{
    public static class ZCurve
    {
        public static T[] Interleave<T>(IReadOnlyList<T> items, int n)
        {
            if (items.Count % n != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(n), "Array must be a multiple of n");
            }
            var output = new T[items.Count];
            var m = output.Length / n;
            for (var i = 0; i < output.Length; i++)
            {
                output[GetInterleavedIndex(i, n, m)] = items[i];
            }
            return output;
        }
        
        public static int GetInterleavedIndex(int idx, int n, int m)
        {
            var i = idx / n;
            var j = idx % n;
            return j * m + i;
        }
    }
}