using System.Collections.Generic;

namespace Eocron.Algorithms.Intervals
{
    public sealed class IntervalPointComparer<T> : IComparer<IntervalPoint<T>>
    {
        public static IntervalPointComparer<T> Default = new IntervalPointComparer<T>(Comparer<T>.Default);

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
            
            return _comparer.Compare(x.Value, y.Value);
        }
    }
}