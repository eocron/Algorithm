using System;
using System.Collections.Generic;

namespace Algorithm.FileCheckSum
{
    /// <summary>
    /// Split stream into chunks of geometric progression size and calculate hash for each one of them.
    /// </summary>
    public class GeometricCheckSum : CheckSumBase
    {
        private readonly int _a;
        private readonly int _q;

        public GeometricCheckSum(int a, int q)
        {
            if (a <= 0)
                throw new ArgumentOutOfRangeException(nameof(a));
            if (q <= 1)
                throw new ArgumentOutOfRangeException(nameof(q));
            _a = a;
            _q = q;
        }
        public override int CalculateCapacity(long streamLength)
        {
            return (int)Math.Ceiling(Math.Log((streamLength / _a) * (_q - 1) + 1, _q));
        }

        public override int CalculatePartSize(IReadOnlyList<int> hashes)
        {
            return (int)(_a * Math.Pow(_q, hashes.Count));
        }
    }
}
