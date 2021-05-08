using System;
using System.Collections.Generic;

namespace Eocron.Algorithms
{
    /// <summary>
    /// Extreme version of byte array equality comparer for x64 environments. 
    /// For equality it uses x64 machine word alignment.
    /// For hash it will scan every 1024 word instead of scanning everything.
    /// Perfromance is just like memcpm.
    /// </summary>
    public class ByteArrayEqualityComparer : IEqualityComparer<ArraySegment<byte>>, IEqualityComparer<byte[]>
    {
        public static readonly ByteArrayEqualityComparer Default = new ByteArrayEqualityComparer();

        private readonly bool _hashWithLoss;
        private const int _hashLossPow = 10;
        private const int _hashLoss = (1 << _hashLossPow) * sizeof(long);
        public ByteArrayEqualityComparer(bool hashWithLoss = true)
        {
            _hashWithLoss = hashWithLoss;
        }

        private unsafe bool EqualBytesLongUnrolled(ArraySegment<byte> data1, ArraySegment<byte> data2)
        {
            if (data1.Count != data2.Count)
                return false;
            fixed (byte* bytes1 = data1.Array, bytes2 = data2.Array)
            {
                int offset = data1.Offset;
                int len = data1.Count;
                int rem = len % (sizeof(long) * 16);
                long* b1 = (long*)(bytes1 + offset);
                long* b2 = (long*)(bytes2 + offset);
                long* e1 = (long*)(bytes1 + offset + len - rem);

                while (b1 < e1)
                {
                    if (*(b1) != *(b2) || *(b1 + 1) != *(b2 + 1) ||
                        *(b1 + 2) != *(b2 + 2) || *(b1 + 3) != *(b2 + 3) ||
                        *(b1 + 4) != *(b2 + 4) || *(b1 + 5) != *(b2 + 5) ||
                        *(b1 + 6) != *(b2 + 6) || *(b1 + 7) != *(b2 + 7) ||
                        *(b1 + 8) != *(b2 + 8) || *(b1 + 9) != *(b2 + 9) ||
                        *(b1 + 10) != *(b2 + 10) || *(b1 + 11) != *(b2 + 11) ||
                        *(b1 + 12) != *(b2 + 12) || *(b1 + 13) != *(b2 + 13) ||
                        *(b1 + 14) != *(b2 + 14) || *(b1 + 15) != *(b2 + 15))
                        return false;
                    b1 += 16;
                    b2 += 16;
                }

                for (int i = 0; i < rem; i++)
                    if (data1[offset + len - 1 - i] != data2[offset + len - 1 - i])
                        return false;

                return true;
            }
        }

        private unsafe long GetHashCodeLongUnrolled(ArraySegment<byte> data)
        {
            var hash = 17L;
            fixed (byte* bytes = data.Array)
                unchecked
                {
                    int offset = data.Offset;
                    int count = data.Count;
                    int rem = count % (sizeof(long));
                    long* b = (long*)(bytes + offset);
                    long* e = (long*)(bytes + offset + count - rem);

                    while (b < e)
                    {
                        hash = hash * 31 + *b;
                        b++;
                    }

                    for (int i = 0; i < rem; i++)
                        hash = hash * 31 + data[offset + count - 1 - i];

                    return hash;
                }
        }

        private unsafe long GetHashCodeLongUnrolledLoss(ArraySegment<byte> data)
        {
            var hash = 17L;
            int offset = data.Offset;
            int count = data.Count;
            var step = count >> _hashLossPow;
            fixed (byte* bytes = data.Array)
                unchecked
                {
                    //skipping everything by step
                    int rem = count % (sizeof(long) * step);
                    long* b = (long*)(bytes + offset);
                    long* e = (long*)(bytes + offset + count - rem);
                    long* e2 = (long*)(bytes + offset + count - sizeof(long));
                    while (b < e)
                    {
                        hash = hash * 31 + *b;
                        b += step;
                    }
                    //checking last one
                    hash = hash * 31 + *e2;
                    return hash;
                }
        }

        public virtual bool Equals(ArraySegment<byte> x, ArraySegment<byte> y)
        {
            if (ReferenceEquals(x.Array, y.Array) && x.Offset == y.Offset && x.Count == y.Count)
                return true;
            if (ReferenceEquals(x.Array, null))
                return false;
            if (ReferenceEquals(y.Array, null))
                return false;
            return EqualBytesLongUnrolled(x, y);
        }

        public virtual int GetHashCode(ArraySegment<byte> obj)
        {
            if (obj == null)
                return 0;
            if (_hashWithLoss && obj.Count > _hashLoss)
                return (int)GetHashCodeLongUnrolledLoss(obj);
            return (int)GetHashCodeLongUnrolled(obj);
        }

        public bool Equals(byte[] x, byte[] y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            return Equals(new ArraySegment<byte>(x), new ArraySegment<byte>(y));
        }

        public int GetHashCode(byte[] obj)
        {
            if (obj == null)
                return 0;
            return GetHashCode(new ArraySegment<byte>(obj));
        }
    }
}
