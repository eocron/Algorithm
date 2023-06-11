using System;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.Algorithms.Intervals
{
    [Obsolete("IN DEVELOPMENT")]
    internal static class IntervalTreeExtensions
    {
        public static void Add<TPoint, TValue>(this IIntervalTree<TPoint, TValue> tree,
            Interval<TPoint> key, TValue value)
        {
            tree.Add(new KeyValuePair<Interval<TPoint>, TValue>(key, value));
        }

        public static void AddRange<TPoint, TValue>(this IIntervalTree<TPoint, TValue> tree,
            IEnumerable<KeyValuePair<Interval<TPoint>, TValue>> items)
        {
            foreach (var keyValuePair in items) tree.Add(keyValuePair);
        }

        public static void AddRange<TPoint, TValue>(this IIntervalTree<TPoint, TValue> tree,
            IEnumerable<Interval<TPoint>> keys, TValue value)
        {
            tree.AddRange(keys.Select(x => new KeyValuePair<Interval<TPoint>, TValue>(x, value)));
        }

        public static bool Remove<TPoint, TValue>(this IIntervalTree<TPoint, TValue> tree,
            Interval<TPoint> key)
        {
            return tree.Remove(new KeyValuePair<Interval<TPoint>, TValue>(key, default));
        }
    }
}