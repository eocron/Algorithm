using System.Collections.Generic;

namespace Eocron.Algorithms.EqualityComparers
{
    /// <summary>
    ///     Checks equality for generic enumerables.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ArrayEqualityComparer<T> : IEqualityComparer<IEnumerable<T>>
    {
        /// <summary>
        ///     Checks equality for generic enumerables.
        /// </summary>
        /// <param name="equalityComparer">Element equality comparer</param>
        public ArrayEqualityComparer(IEqualityComparer<T> equalityComparer = null)
        {
            _equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
        }

        public bool Equals(IEnumerable<T> x, IEnumerable<T> y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;

            using var xx = x.GetEnumerator();
            using var yy = y.GetEnumerator();
            while (true)
            {
                var mx = xx.MoveNext();
                var my = yy.MoveNext();
                if (mx != my)
                    return false;
                if (!mx)
                    break;
                if (!_equalityComparer.Equals(xx.Current, yy.Current))
                    return false;
            }

            return true;
        }

        public int GetHashCode(IEnumerable<T> obj)
        {
            if (obj == null)
                return 0;

            var hash = 17;

            unchecked
            {
                foreach (var e in obj) hash = hash * 31 + _equalityComparer.GetHashCode(e);
            }

            return hash;
        }

        public static readonly ArrayEqualityComparer<T> Default = new ArrayEqualityComparer<T>();

        private readonly IEqualityComparer<T> _equalityComparer;
    }
}