using System;
using System.Collections.Generic;

namespace Eocron.Algorithms.Intervals
{
    public readonly struct IntervalPoint<T> : IEquatable<IntervalPoint<T>>
    {
        public readonly T Value;
        public readonly bool IsGougedOut;
        public readonly bool IsNegativeInfinity;
        public readonly bool IsPositiveInfinity;

        public bool IsInfinity => IsPositiveInfinity || IsNegativeInfinity;

        public IntervalPoint(T value, bool isGougedOut)
        {
            Value = value;
            IsGougedOut = isGougedOut;
            IsNegativeInfinity = false;
            IsPositiveInfinity = false;
        }

        private IntervalPoint(bool isNegativeInfinity, bool isPositiveInfinity)
        {
            Value = default(T);
            IsGougedOut = true;
            IsNegativeInfinity = isNegativeInfinity;
            IsPositiveInfinity = isPositiveInfinity;
        }

        public override string ToString()
        {
            if (IsNegativeInfinity)
                return "-inf";
            if (IsPositiveInfinity)
                return "+inf";
            return Value.ToString();
        }

        public static readonly IntervalPoint<T> PositiveInfinity = new IntervalPoint<T>(false, true);
        public static readonly IntervalPoint<T> NegativeInfinity = new IntervalPoint<T>(true, false);

        public bool Equals(IntervalPoint<T> other)
        {
            return EqualityComparer<T>.Default.Equals(Value, other.Value) && IsGougedOut == other.IsGougedOut && IsNegativeInfinity == other.IsNegativeInfinity && IsPositiveInfinity == other.IsPositiveInfinity;
        }

        public override bool Equals(object obj)
        {
            return obj is IntervalPoint<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, IsGougedOut, IsNegativeInfinity, IsPositiveInfinity);
        }
    }
}