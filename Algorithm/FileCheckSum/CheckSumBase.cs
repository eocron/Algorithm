using System;
using System.Collections.Generic;
using System.Text;

namespace Algorithm.FileCheckSum
{
    public abstract class CheckSumBase : ICheckSum<int>
    {
        public virtual int CalculateCapacity(long streamLength)
        {
            return 1;
        }

        public abstract int CalculatePartSize(IReadOnlyList<int> hashes);

        public int InitialHash()
        {
            return 17;
        }


        public int NextHash(int hash, byte[] readBytes, int offset, int count)
        {
            unchecked
            {
                for (var i = 0; i < count; i++)
                {
                    hash = hash * 31 + readBytes[i + offset];
                }
            }
            return hash;
        }
    }
}
