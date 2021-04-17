using System;
using System.Collections.Generic;
using System.Text;

namespace Eocron.Algorithms.FileCheckSum
{
    public class LazyCheckSumEqualityComparer<T> : IEqualityComparer<ILazyCheckSum<T>>
    {
        private readonly IEqualityComparer<T> _hashComparer;

        public LazyCheckSumEqualityComparer(IEqualityComparer<T> hashComparer = null)
        {
            _hashComparer = hashComparer ?? EqualityComparer<T>.Default;
        }
        public bool Equals(ILazyCheckSum<T> x, ILazyCheckSum<T> y)
        {
            if (object.ReferenceEquals(x, y))
                return true;
            if (object.ReferenceEquals(x, null))
                return false;

            using var xe = x.GetEnumerator();
            using var ye = y.GetEnumerator();
            while (true)
            {
                var mvx = xe.MoveNext();
                var mvy = ye.MoveNext();
                if (mvx != mvy)
                    return false;
                if (mvx == false)
                    break;
                if (!_hashComparer.Equals(xe.Current, ye.Current))
                    return false;
            }
            return true;
        }

        public int GetHashCode(ILazyCheckSum<T> obj)
        {
            throw new NotImplementedException();
        }
    }
}
