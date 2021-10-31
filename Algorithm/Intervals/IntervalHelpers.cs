using System;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.Algorithms.Intervals
{
    public static class IntervalHelpers
    {
        public static bool Contains<T>(Interval<T> interval, IntervalPoint<T> value, IComparer<IntervalPoint<T>> comparer)
        {
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
        public static bool Overlaps<T>(Interval<T> a, Interval<T> b, IComparer<IntervalPoint<T>> comparer)
        {
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
        public static bool Touches<T>(Interval<T> a, Interval<T> b, IComparer<IntervalPoint<T>> comparer)
        {
            return comparer.Compare(a.StartPoint, b.EndPoint) == 0 && a.StartPoint.IsGougedOut ^ b.EndPoint.IsGougedOut ||
                   comparer.Compare(a.EndPoint, b.StartPoint) == 0 && a.EndPoint.IsGougedOut ^ b.StartPoint.IsGougedOut;
        }

        public static List<Interval<T>> Union<T>(IEnumerable<Interval<T>> intervals,
            IComparer<IntervalPoint<T>> comparer)
        {
            if (intervals == null)
                throw new ArgumentNullException(nameof(intervals));
            var result = new List<Interval<T>>();


            foreach (var interval in intervals)
            {
                int idx = -1;
                for(int i = 0; i < result.Count; i++)
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
                        Min(result[idx].StartPoint, interval.StartPoint, comparer),
                        Max(result[idx].EndPoint, interval.EndPoint, comparer),
                        comparer);
                }
                else
                {
                    result.Add(interval);
                }
            }


            return result;
        }
        public static List<Interval<T>> Intersect<T>(IEnumerable<Interval<T>> intervals, IComparer<IntervalPoint<T>> comparer)
        {
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

                l = Max(l, curr.StartPoint, comparer);
                r = Min(r, curr.EndPoint, comparer);
            }


            return new List<Interval<T>>() {Interval<T>.Create(l, r, comparer)};
        }

        public static IntervalPoint<T> Max<T>(IntervalPoint<T> a, IntervalPoint<T> b,
            IComparer<IntervalPoint<T>> comparer)
        {
            var cmp = new IntervalGougedPointComparer<T>(comparer).Compare(a, b);
            return cmp >= 0 ? a : b;
        }

        public static IntervalPoint<T> Min<T>(IntervalPoint<T> a, IntervalPoint<T> b,
            IComparer<IntervalPoint<T>> comparer)
        {
            var cmp = new IntervalGougedPointComparer<T>(comparer).Compare(a, b);
            return cmp <= 0 ? a : b;
        }


        public static IntervalPoint<T> Negate<T>(IntervalPoint<T> point)
        {
            return new IntervalPoint<T>(point.Value, !point.IsGougedOut);
        }
        public static List<Interval<T>> Negate<T>(Interval<T> interval, IComparer<IntervalPoint<T>> comparer)
        {
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
        /// Subtracts one intervals from another.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="comparer"></param>
        /// <returns>Zero or One or Two intervals.</returns>
        public static List<Interval<T>> Except<T>(Interval<T> a, Interval<T> b, IComparer<IntervalPoint<T>> comparer)
        {
            var negated = Negate(b, comparer);
            var result =
                Union(
                    negated.SelectMany(x => Intersect(new[] {x, a}, comparer)), comparer);
            return result;
        }
    }
}