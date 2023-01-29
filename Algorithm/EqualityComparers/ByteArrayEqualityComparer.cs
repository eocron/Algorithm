using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Eocron.Algorithms
{
    /// <summary>
    /// Extreme version of byte array equality comparer for x64 environments. 
    /// Perfromance is just like memcpm.
    /// </summary>
    public sealed class ByteArrayEqualityComparer : IEqualityComparer<ArraySegment<byte>>, IEqualityComparer<byte[]>
    {
        public static readonly ByteArrayEqualityComparer Default = new ByteArrayEqualityComparer();

        private readonly bool _hashWithLoss;
        private const int _hashLossPow = 10;
        private const int _hashLoss = (1 << _hashLossPow) * sizeof(long);

        public ByteArrayEqualityComparer(bool hashWithLoss = true)
        {
            _hashWithLoss = hashWithLoss;
        }

        public int GetHashCode(ArraySegment<byte> obj)
        {
            if (obj == null)
                return 0;
            if (_hashWithLoss && obj.Count > _hashLoss)
                return (int)GetHashCodeLoss(obj);
            const int sizeBorder = sizeof(ulong) * 8;
            return obj.Count < sizeBorder ? (int)GetHashCode128Bit(obj) : (int)GetHashCode512Bit(obj);
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

        public bool Equals(ArraySegment<byte> x, ArraySegment<byte> y)
        {
            if (ReferenceEquals(x.Array, y.Array) && x.Offset == y.Offset && x.Count == y.Count)
                return true;
            if (ReferenceEquals(x.Array, null))
                return false;
            if (ReferenceEquals(y.Array, null))
                return false;
            if (x.Count != x.Count)
                return false;
            const int sizeBorder = sizeof(ulong) * 8;
            return x.Count < sizeBorder ? Equal128Bit(x, y) : Equal512Bit(x, y);
        }

        private static unsafe bool Equal128Bit(ArraySegment<byte> data1, ArraySegment<byte> data2)
        {
            const int step = 2;
            const int stepSize = sizeof(ulong) * step;
            fixed (byte* bytes1 = data1.Array, bytes2 = data2.Array)
            {
                int tail = data1.Count % stepSize;
                ulong* b1 = (ulong*)(bytes1 + data1.Offset);
                ulong* b2 = (ulong*)(bytes2 + data1.Offset);
                ulong* e1 = (ulong*)(bytes1 + data1.Offset + data1.Count - tail);

                while (b1 < e1)
                {
                    if (*b1 != *b2 ||
                        *(b1 + 1) != *(b2 + 1))
                        return false;
                    b1 += step;
                    b2 += step;
                }

                for (int i = data1.Count - tail; i < data1.Count; i++)
                    if (data1[i] != data2[i])
                        return false;

                return true;
            }
        }

        private static unsafe bool Equal512Bit(ArraySegment<byte> data1, ArraySegment<byte> data2)
        {
            const int step = 8;
            const int stepSize = sizeof(ulong) * step;
            fixed (byte* bytes1 = data1.Array, bytes2 = data2.Array)
            {
                int tail = data1.Count % stepSize;
                ulong* b1 = (ulong*)(bytes1 + data1.Offset);
                ulong* b2 = (ulong*)(bytes2 + data1.Offset);
                ulong* e1 = (ulong*)(bytes1 + data1.Offset + data1.Count - tail);

                while (b1 < e1)
                {
                    if (*b1 != *b2 ||
                        *(b1 + 1) != *(b2 + 1) ||
                        *(b1 + 2) != *(b2 + 2) ||
                        *(b1 + 3) != *(b2 + 3) ||
                        *(b1 + 4) != *(b2 + 4) ||
                        *(b1 + 5) != *(b2 + 5) ||
                        *(b1 + 6) != *(b2 + 6) ||
                        *(b1 + 7) != *(b2 + 7))
                        return false;
                    b1 += step;
                    b2 += step;
                }

                for (int i = data1.Count - tail; i < data1.Count; i++)
                    if (data1[i] != data2[i])
                        return false;

                return true;
            }
        }

        private static unsafe ulong GetHashCode128Bit(ArraySegment<byte> source)
        {
            ulong hash1 = 31 ^ (ulong)source.Count;
            ulong hash2 = 37;
            const int step = 2;
            const int stepSize = sizeof(ulong) * step;
            fixed (byte* pSource = source.Array)
                unchecked
                {
                    int tail = source.Count % stepSize;
                    ulong* b = (ulong*)(pSource + source.Offset);
                    ulong* e = (ulong*)(pSource + source.Offset + source.Count - tail);

                    while (b < e)
                    {
                        hash1 = Rehash(hash1) ^ *b;
                        hash2 = Rehash(hash2) ^ *(b + 1);
                        b += step;
                    }

                    for (int i = source.Count - tail; i < source.Count; i++)
                        hash1 = Rehash(hash1) ^ source[i];
                }

            return hash1 ^ hash2;
        }

        private static unsafe ulong GetHashCode512Bit(ArraySegment<byte> source)
        {
            ulong hash1 = Rehash(17) ^ (ulong)source.Count;
            ulong hash2 = 3;
            ulong hash3 = 5;
            ulong hash4 = 7;
            ulong hash5 = 11;
            ulong hash6 = 13;
            ulong hash7 = 17;
            ulong hash8 = 19;
            const int step = 8;
            const int stepSize = sizeof(ulong) * step;
            fixed (byte* pSource = source.Array)
                unchecked
                {
                    int tail = source.Count % stepSize;
                    ulong* b = (ulong*)(pSource + source.Offset);
                    ulong* e = (ulong*)(pSource + source.Offset + source.Count - tail);

                    while (b < e)
                    {
                        hash1 = Rehash(hash1) ^ *b;
                        hash2 = Rehash(hash2) ^ *(b + 1);
                        hash3 = Rehash(hash3) ^ *(b + 2);
                        hash4 = Rehash(hash4) ^ *(b + 3);
                        hash5 = Rehash(hash5) ^ *(b + 4);
                        hash6 = Rehash(hash6) ^ *(b + 5);
                        hash7 = Rehash(hash7) ^ *(b + 6);
                        hash8 = Rehash(hash8) ^ *(b + 7);
                        b += step;
                    }

                    for (int i = source.Count - tail; i < source.Count; i++)
                        hash1 = Rehash(hash1) ^ source[i];
                }

            return hash1 ^ hash2 ^ hash3 ^ hash4 ^ hash5 ^ hash6 ^ hash7 ^ hash8;
        }

        private static unsafe ulong GetHashCodeLoss(ArraySegment<byte> source)
        {
            ulong hash = 17L;
            var step = (source.Count >> _hashLossPow);
            var stepSize = sizeof(ulong) * step;
            fixed (byte* pSource = source.Array)
                unchecked
                {
                    int tail = source.Count % stepSize;
                    ulong* b = (ulong*)(pSource + source.Offset);
                    ulong* e = (ulong*)(pSource + source.Offset + source.Count - tail);
                    ulong* e2 = (ulong*)(pSource + source.Offset + source.Count - sizeof(ulong));
                    while (b < e)
                    {
                        hash = Rehash(hash) ^ *b;
                        b += step;
                    }

                    hash = Rehash(hash) ^ *e2;
                    return hash;
                }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Rehash(ulong x)
        {
            return ((x << 5) | (x >> 63)) + x;
        }
    }
}
