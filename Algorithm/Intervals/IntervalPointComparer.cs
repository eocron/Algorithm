using System.Collections.Generic;

namespace Eocron.Algorithms.Intervals
{
    public sealed class IntervalGougedPointComparer<T> : IComparer<IntervalPoint<T>>
    {
        private readonly IComparer<IntervalPoint<T>> _comparerImplementation;

        public IntervalGougedPointComparer(IComparer<IntervalPoint<T>> comparerImplementation)
        {
            _comparerImplementation = comparerImplementation;
        }

        public int Compare(IntervalPoint<T> x, IntervalPoint<T> y)
        {
            var cmp = _comparerImplementation.Compare(x, y);
            if (cmp != 0)
                return cmp;

            var gouge = y.IsGougedOut.CompareTo(x.IsGougedOut);
            return gouge;
        }
    }

    public sealed class IntervalPointComparer<T> : IComparer<IntervalPoint<T>>
    {
        private readonly IComparer<T> _comparer;

        public IntervalPointComparer(IComparer<T> comparer)
        {
            _comparer = comparer;
        }

        public int Compare(IntervalPoint<T> x, IntervalPoint<T> y)
        {
            //for intervals this assumption is correct about infinities, because they represent not arithmetic infinity, but border, i.e (left border, right border).
            if (x.IsNegativeInfinity && y.IsNegativeInfinity)
                return 0;
            if (x.IsPositiveInfinity && y.IsPositiveInfinity)
                return 0;
            if (x.IsNegativeInfinity || y.IsPositiveInfinity)
                return -1;
            if (x.IsPositiveInfinity || y.IsNegativeInfinity)
                return 1;

            //var gougedCmp = y.IsGougedOut.CompareTo(x.IsGougedOut);
            var valueCmp = _comparer.Compare(x.Value, y.Value);
            //if (gougedCmp != 0 && valueCmp == 0)
            //    return gougedCmp;
            return valueCmp;
        }
    }
}