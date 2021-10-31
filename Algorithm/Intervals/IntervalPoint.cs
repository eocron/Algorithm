using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Eocron.Algorithms.Intervals
{
    [DataContract]
    public readonly struct IntervalPoint<T> : IEquatable<IntervalPoint<T>>
    {
        public static readonly IntervalPoint<T> PositiveInfinity = new IntervalPoint<T>(false, true);
        public static readonly IntervalPoint<T> NegativeInfinity = new IntervalPoint<T>(true, false);

        [DataMember(Order = 1)]
        public readonly T Value;
        [DataMember(Order = 2, EmitDefaultValue = false)]
        public readonly bool IsGougedOut;
        [DataMember(Order = 3, EmitDefaultValue = false)]
        public readonly bool IsNegativeInfinity;
        [DataMember(Order = 4, EmitDefaultValue = false)]
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