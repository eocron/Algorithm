using System;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.Algorithms.Intervals
{

    public static class Interval
    {
        public static Interval<T> Create<T>(T start, T end) where T : struct
        {
            return Interval<T>.Create(
                new IntervalPoint<T>(start, false),
                new IntervalPoint<T>(end, false),
                new IntervalPointComparer<T>(Comparer<T>.Default));
        }

        public static Interval<T> Create<T>(T? start, T? end) where T : struct
        {
            return Interval<T>.Create(
                start == null ? IntervalPoint<T>.NegativeInfinity : new IntervalPoint<T>(start.Value, false),
                end == null ? IntervalPoint<T>.PositiveInfinity : new IntervalPoint<T>(end.Value, false),
                IntervalPointComparer<T>.Default);
        }

        public static bool Contains<T>(this Interval<T> interval, T value)
        {
            return interval.Contains(new IntervalPoint<T>(value, false));
        }

        public static bool Contains<T>(this Interval<T> interval, IntervalPoint<T> value, IComparer<IntervalPoint<T>> comparer= null)
        {
            comparer ??= IntervalPointComparer<T>.Default;
            var startCmp = comparer.Compare(value, interval.StartPoint);
            if (startCmp < 0 || startCmp == 0 && (interval.StartPoint.IsGougedOut || value.IsGougedOut))
                return false;

            var endCmp = comparer.Compare(value, interval.EndPoint);
            if (endCmp > 0 || endCmp == 0 && (interval.EndPoint.IsGougedOut || value.IsGougedOut))
                return false;
            return true;
        }


        /// <summary>
        /// Check if intervals overlap.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="comparer"></param>
        /// <returns>True if overlap, false otherwise.</returns>
        public static bool Overlaps<T>(this Interval<T> a, Interval<T> b, IComparer<IntervalPoint<T>> comparer = null)
        {
            comparer ??= IntervalPointComparer<T>.Default;
            if (Contains(b, a.StartPoint, comparer))
                return true;
            if (Contains(b, a.EndPoint, comparer))
                return true;
            return false;
        }

        /// <summary>
        /// Check if intervals touch each other. I.e, there is no more domain points between them.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static bool Touches<T>(this Interval<T> a, Interval<T> b, IComparer<IntervalPoint<T>> comparer = null)
        {
            comparer ??= IntervalPointComparer<T>.Default;
            return comparer.Compare(a.StartPoint, b.EndPoint) == 0 && a.StartPoint.IsGougedOut ^ b.EndPoint.IsGougedOut ||
                   comparer.Compare(a.EndPoint, b.StartPoint) == 0 && a.EndPoint.IsGougedOut ^ b.StartPoint.IsGougedOut;
        }

        /// <summary>
        /// A | B | C | ... is the set of all values which members of A or B or C or ...
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="intervals"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static IEnumerable<Interval<T>> Union<T>(this IEnumerable<Interval<T>> intervals, IComparer<IntervalPoint<T>> comparer = null)
        {
            comparer ??= IntervalPointComparer<T>.Default;
            if (intervals == null)
                throw new ArgumentNullException(nameof(intervals));

            var orderedIntervals = intervals.OrderBy(x => x.StartPoint, new IntervalGougedPointComparer<T>(comparer, true));
            using var enumerator = orderedIntervals.GetEnumerator();
            if(!enumerator.MoveNext())
                yield break;

            var prev = enumerator.Current;
            while (enumerator.MoveNext())
            {
                var interval = enumerator.Current;
                if(Overlaps(prev, interval, comparer) || Touches(prev, interval, comparer))
                    prev = Interval<T>.Create(
                        Min(prev.StartPoint, interval.StartPoint, true, comparer),
                        Max(prev.EndPoint, interval.EndPoint, false, comparer),
                        comparer);
                else
                {
                    yield return prev;
                    prev = interval;
                }
            }

            yield return prev;
        }

        /// <summary>
        /// A & B & C & ... is the set of all values which members of A and B and C and ...
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="intervals"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static IEnumerable<Interval<T>> Intersection<T>(this IEnumerable<Interval<T>> intervals, IComparer<IntervalPoint<T>> comparer = null)
        {
            comparer ??= IntervalPointComparer<T>.Default;
            if (intervals == null)
                throw new ArgumentNullException(nameof(intervals));
            using var enumerator = intervals.GetEnumerator();
            if (!enumerator.MoveNext())
                yield break;

            var l = enumerator.Current.StartPoint;
            var r = enumerator.Current.EndPoint;

            while (enumerator.MoveNext())
            {
                var curr = enumerator.Current;
                var startCmp = comparer.Compare(curr.StartPoint, r);
                var endCmp = comparer.Compare(curr.EndPoint, l);
                if (startCmp > 0 || startCmp == 0 && (curr.StartPoint.IsGougedOut || r.IsGougedOut) ||
                    endCmp < 0 || endCmp == 0 && (curr.EndPoint.IsGougedOut || l.IsGougedOut))
                    yield break;

                l = Max(l, curr.StartPoint, true, comparer);
                r = Min(r, curr.EndPoint, false, comparer);
            }
            yield return Interval<T>.Create(l, r, comparer);
        }

        /// <summary>
        /// ~A is the set of all values that are not members of A
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="interval"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static IEnumerable<Interval<T>> Complement<T>(this Interval<T> interval, IComparer<IntervalPoint<T>> comparer = null)
        {
            comparer ??= IntervalPointComparer<T>.Default;
            if (!interval.StartPoint.IsNegativeInfinity)
            {
                yield return Interval<T>.Create(
                    IntervalPoint<T>.NegativeInfinity,
                    Complement(interval.StartPoint),
                    comparer);
            }
            if (!interval.EndPoint.IsPositiveInfinity)
            {
                yield return Interval<T>.Create(
                    Complement(interval.EndPoint),
                    IntervalPoint<T>.PositiveInfinity,
                    comparer);
            }
        }

        /// <summary>
        /// A \ B, is the set of all values of A that are not members of B
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a">A</param>
        /// <param name="b">B</param>
        /// <param name="comparer"></param>
        /// <returns>Zero or One or Two intervals.</returns>
        public static IEnumerable<Interval<T>> Difference<T>(this Interval<T> a, Interval<T> b, IComparer<IntervalPoint<T>> comparer = null)
        {
            comparer ??= IntervalPointComparer<T>.Default;
            return Union(Complement(b, comparer).SelectMany(x => Intersection(new[] { x, a }, comparer)), comparer);
        }

        /// <summary>
        /// A ^ B, is the set of all values which are in one of the sets, but not both
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a">A</param>
        /// <param name="b">B</param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static IEnumerable<Interval<T>> SymmetricDifference<T>(this Interval<T> a, Interval<T> b,
            IComparer<IntervalPoint<T>> comparer = null)
        {
            comparer ??= IntervalPointComparer<T>.Default;
            return Union(Difference(a, b, comparer).Union(Difference(b, a, comparer)), comparer);
        }

        internal static IntervalPoint<T> Max<T>(IntervalPoint<T> a, IntervalPoint<T> b, bool isLeftGouged,
            IComparer<IntervalPoint<T>> comparer)
        {
            var cmp = new IntervalGougedPointComparer<T>(comparer, isLeftGouged).Compare(a, b);
            return cmp >= 0 ? a : b;
        }

        internal static IntervalPoint<T> Min<T>(IntervalPoint<T> a, IntervalPoint<T> b, bool isLeftGouged,
            IComparer<IntervalPoint<T>> comparer)
        {
            var cmp = new IntervalGougedPointComparer<T>(comparer, isLeftGouged).Compare(a, b);
            return cmp <= 0 ? a : b;
        }

        private static IntervalPoint<T> Complement<T>(IntervalPoint<T> point)
        {
            return new IntervalPoint<T>(point.Value, !point.IsGougedOut);
        }
    }
}