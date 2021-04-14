using System;
using System.Collections.Generic;

namespace Algorithm.FileCheckSum
{
    /// <summary>
    /// Split stream into chunks of equal size and calculate hash for each one of them.
    /// </summary>
    public class PartitionedCheckSum : CheckSumBase
    {
        private readonly int _partSize;

        public PartitionedCheckSum(int partSize)
        {
            if (partSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(partSize));
            _partSize = partSize;
        }
        public override int CalculateCapacity(long streamLength)
        {
            return (int)(streamLength / _partSize) + ((streamLength % _partSize) > 0 ? 1 : 0);
        }

        public override int CalculatePartSize(IReadOnlyList<int> hashes)
        {
            return _partSize;
        }
    }
}
