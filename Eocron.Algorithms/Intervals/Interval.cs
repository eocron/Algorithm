using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Eocron.Algorithms.Intervals
{
    [DataContract]
    public readonly struct Interval<T> : IEquatable<Interval<T>>
    {
        [DataMember(Order = 1, Name = "s")]
        public readonly IntervalPoint<T> StartPoint;

        [DataMember(Order = 2, Name = "e")]
        public readonly IntervalPoint<T> EndPoint;

        [IgnoreDataMember]
        public bool HasStart => !StartPoint.IsInfinity;
        [IgnoreDataMember]
        public bool HasEnd => !EndPoint.IsInfinity;

        private Interval(IntervalPoint<T> startPoint, IntervalPoint<T> endPoint)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
        }

        public static Interval<T> Create(IntervalPoint<T> startPoint, IntervalPoint<T> endPoint, IComparer<IntervalPoint<T>> comparer = null)
        {
            comparer = comparer ?? IntervalPointComparer<T>.Default;
            var cmp = comparer.Compare(startPoint, endPoint);
            if (cmp > 0)
                throw new ArgumentOutOfRangeException(nameof(startPoint), "startPoint should be lower or equal to endPoint");
            if (cmp == 0 && startPoint.IsGougedOut ^ endPoint.IsGougedOut)
                throw new ArgumentOutOfRangeException(nameof(startPoint),
                    "Single point with different gouge out flag is invalid.");

            return new Interval<T>(startPoint, endPoint);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(StartPoint.IsGougedOut || StartPoint.IsInfinity ? "(" : "[");
            sb.Append(StartPoint);

            sb.Append(";");
            sb.Append(EndPoint);
            sb.Append(EndPoint.IsGougedOut || EndPoint.IsInfinity ? ")" : "]");
            return sb.ToString();
        }

        public bool Equals(Interval<T> other)
        {
            return StartPoint.Equals(other.StartPoint) && EndPoint.Equals(other.EndPoint);
        }

        public override bool Equals(object obj)
        {
            return obj is Interval<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(StartPoint, EndPoint);
        }
    }
}
