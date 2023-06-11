using System.Collections.Generic;

namespace Eocron.Algorithms.Intervals
{
    internal sealed class IntervalGougedPointComparer<T> : IComparer<IntervalPoint<T>>
    {
        private readonly IComparer<IntervalPoint<T>> _comparerImplementation;
        private readonly bool _isLeftGouged;

        public IntervalGougedPointComparer(IComparer<IntervalPoint<T>> comparerImplementation, bool isLeftGouged)
        {
            _comparerImplementation = comparerImplementation;
            _isLeftGouged = isLeftGouged;//gouged on left side or on right side?
        }

        public int Compare(IntervalPoint<T> x, IntervalPoint<T> y)
        {
            var cmp = _comparerImplementation.Compare(x, y);
            if (cmp != 0)
                return cmp;

            var gouge = _isLeftGouged ? x.IsGougedOut.CompareTo(y.IsGougedOut) : y.IsGougedOut.CompareTo(x.IsGougedOut);
            return gouge;
        }
    }
}