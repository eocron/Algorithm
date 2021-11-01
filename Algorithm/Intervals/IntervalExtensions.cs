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
        public static List<Interval<T>> Union<T>(this IEnumerable<Interval<T>> intervals, IComparer<IntervalPoint<T>> comparer = null)
        {
            comparer ??= IntervalPointComparer<T>.Default;

            if (intervals == null)
                throw new ArgumentNullException(nameof(intervals));
            var result = new List<Interval<T>>();


            foreach (var interval in intervals)
            {
                int idx = -1;
                for (int i = 0; i < result.Count; i++)
                {
                    var candidateToUnite = result[i];
                    if (Overlaps(candidateToUnite, interval, comparer) || Touches(candidateToUnite, interval, comparer))
                    {
                        idx = i;
                        break;
                    }
                }

                if (idx >= 0)
                {
                    result[idx] = Interval<T>.Create(
                        Min(result[idx].StartPoint, interval.StartPoint, true, comparer),
                        Max(result[idx].EndPoint, interval.EndPoint, false, comparer),
                        comparer);
                }
                else
                {
                    result.Add(interval);
                }
            }


            return result;
        }

        /// <summary>
        /// A & B & C & ... is the set of all values which members of A and B and C and ...
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="intervals"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static List<Interval<T>> Intersection<T>(this IEnumerable<Interval<T>> intervals, IComparer<IntervalPoint<T>> comparer = null)
        {
            comparer ??= IntervalPointComparer<T>.Default;
            if (intervals == null)
                throw new ArgumentNullException(nameof(intervals));
            using var enumerator = intervals.GetEnumerator();
            if (!enumerator.MoveNext())
                throw new ArgumentOutOfRangeException("No intervals provided.");

            IntervalPoint<T> l = enumerator.Current.StartPoint;
            IntervalPoint<T> r = enumerator.Current.EndPoint;

            while (enumerator.MoveNext())
            {
                var curr = enumerator.Current;
                var startCmp = comparer.Compare(curr.StartPoint, r);
                var endCmp = comparer.Compare(curr.EndPoint, l);
                if (startCmp > 0 || startCmp == 0 && (curr.StartPoint.IsGougedOut || r.IsGougedOut) ||
                    endCmp < 0 || endCmp == 0 && (curr.EndPoint.IsGougedOut || l.IsGougedOut))
                {
                    return new List<Interval<T>>();
                }

                l = Max(l, curr.StartPoint, true, comparer);
                r = Min(r, curr.EndPoint, false, comparer);
            }


            return new List<Interval<T>>() { Interval<T>.Create(l, r, comparer) };
        }

        /// <summary>
        /// ~A is the set of all values that are not members of A
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="interval"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static List<Interval<T>> Complement<T>(this Interval<T> interval, IComparer<IntervalPoint<T>> comparer = null)
        {
            comparer ??= IntervalPointComparer<T>.Default;
            var result = new List<Interval<T>>(2);
            if (!interval.StartPoint.IsNegativeInfinity)
            {
                result.Add(Interval<T>.Create(
                    IntervalPoint<T>.NegativeInfinity,
                    Negate(interval.StartPoint),
                    comparer));
            }
            if (!interval.EndPoint.IsPositiveInfinity)
            {
                result.Add(Interval<T>.Create(
                    Negate(interval.EndPoint),
                    IntervalPoint<T>.PositiveInfinity,
                    comparer));
            }

            return result;
        }

        /// <summary>
        /// A \ B, is the set of all values of A that are not members of B
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a">A</param>
        /// <param name="b">B</param>
        /// <param name="comparer"></param>
        /// <returns>Zero or One or Two intervals.</returns>
        public static List<Interval<T>> Difference<T>(this Interval<T> a, Interval<T> b, IComparer<IntervalPoint<T>> comparer = null)
        {
            comparer ??= IntervalPointComparer<T>.Default;
            var negated = Complement(b, comparer);
            var intersected = negated.SelectMany(x => Intersection(new[] {x, a}, comparer));
            return Union(intersected, comparer);
        }

        /// <summary>
        /// A ^ B, is the set of all values which are in one of the sets, but not both
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a">A</param>
        /// <param name="b">B</param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static List<Interval<T>> SymmetricDifference<T>(this Interval<T> a, Interval<T> b,
            IComparer<IntervalPoint<T>> comparer = null)
        {
            comparer ??= IntervalPointComparer<T>.Default;
            var left = Difference(a, b, comparer);
            var right = Difference(b, a, comparer);
            return Union(left.Union(right), comparer);
        }

        private static IntervalPoint<T> Max<T>(IntervalPoint<T> a, IntervalPoint<T> b, bool isLeftGouged,
            IComparer<IntervalPoint<T>> comparer)
        {
            var cmp = new IntervalGougedPointComparer<T>(comparer, isLeftGouged).Compare(a, b);
            return cmp >= 0 ? a : b;
        }

        private static IntervalPoint<T> Min<T>(IntervalPoint<T> a, IntervalPoint<T> b, bool isLeftGouged,
            IComparer<IntervalPoint<T>> comparer)
        {
            var cmp = new IntervalGougedPointComparer<T>(comparer, isLeftGouged).Compare(a, b);
            return cmp <= 0 ? a : b;
        }

        private static IntervalPoint<T> Negate<T>(IntervalPoint<T> point)
        {
            return new IntervalPoint<T>(point.Value, !point.IsGougedOut);
        }
    }
}