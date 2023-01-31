using System;
using System.Collections.Generic;
using System.Linq;
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
                return Squash(GetHashCodeLoss(obj));
            const int sizeBorder = sizeof(ulong) * 8;
            return Squash(obj.Count < sizeBorder ? GetHashCode128Bit(obj) : GetHashCode512Bit(obj));
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
            if (x.Count != y.Count)
                return false;
            return Equal512Bit(x, y);
        }

        private static unsafe bool Equal128Bit(ArraySegment<byte> data1, ArraySegment<byte> data2)
        {
            const int step = 2;
            const int stepSize = sizeof(ulong) * step;
            fixed (byte* bytes1 = data1.Array, bytes2 = data2.Array)
            {
                int tail = data1.Count % stepSize;
                ulong* b1 = (ulong*)(bytes1 + data1.Offset);
                ulong* b2 = (ulong*)(bytes2 + data2.Offset);
                ulong* e1 = (ulong*)(bytes1 + data1.Offset + data1.Count - tail);

                while (b1 < e1)
                {
                    if (*b1 != *b2 ||
                        *(b1 + 1) != *(b2 + 1))
                        return false;
                    b1 += step;
                    b2 += step;
                }

                return tail == 0 || Equals128BitTail(*b1, *(b1+1), *b2, *(b2+1), tail << 3);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equals128BitTail(ulong a1, ulong a2, ulong b1, ulong b2, int nBits)
        {
            var mask1 = nBits >= 64 ? ulong.MaxValue : ulong.MaxValue >> (64 - nBits);
            var mask2 = nBits > 64 ? ulong.MaxValue >> (128 - nBits) : 0;
            return (a1 & mask1) == (b1 & mask1)  && (a2 & mask2) == (b2 & mask2) ;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong GetHashCode128BitTail(ulong hash, ulong a1, ulong a2, int nBits)
        {
            var mask1 = nBits >= 64 ? ulong.MaxValue : ulong.MaxValue >> (64 - nBits);
            var mask2 = nBits > 64 ? ulong.MaxValue >> (128 - nBits) : 0;
            hash = MultiplyBy2147483647AndAdd(hash, a1 & mask1);
            hash = MultiplyBy2147483647AndAdd(hash, a2 & mask2);
            return hash;
        }

        private static unsafe bool Equal512Bit(ArraySegment<byte> data1, ArraySegment<byte> data2)
        {
            const int step = 8;
            const int stepSize = sizeof(ulong) * step;
            fixed (byte* bytes1 = data1.Array, bytes2 = data2.Array)
            {
                int tail = data1.Count % stepSize;
                ulong* b1 = (ulong*)(bytes1 + data1.Offset);
                ulong* b2 = (ulong*)(bytes2 + data2.Offset);
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

                return tail == 0 || Equal128Bit(data1.Slice(data1.Count - tail, tail), data2.Slice(data2.Count - tail, tail));
            }
        }

        private static unsafe ulong GetHashCode128Bit(ArraySegment<byte> source)
        {
            ulong hash1 = 31 + (ulong)source.Count;
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
                        hash1 = MultiplyBy2147483647AndAdd(hash1, *b);
                        hash2 = MultiplyBy2147483647AndAdd(hash2, *(b + 1));
                        b += step;
                    }

                    hash1 = tail == 0 ? hash1 : GetHashCode128BitTail(hash1, *b, *(b + 1), tail << 3);
                }

            return MultiplyBy31AndAdd(hash1, hash2);
        }

        private static unsafe ulong GetHashCode512Bit(ArraySegment<byte> source)
        {
            ulong hash1 = 17 + (ulong)source.Count;
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
                        hash1 = MultiplyBy2147483647AndAdd(hash1, *b);
                        hash2 = MultiplyBy2147483647AndAdd(hash2, *(b + 1));
                        hash3 = MultiplyBy2147483647AndAdd(hash3, *(b + 2));
                        hash4 = MultiplyBy2147483647AndAdd(hash4, *(b + 3));
                        hash5 = MultiplyBy2147483647AndAdd(hash5, *(b + 4));
                        hash6 = MultiplyBy2147483647AndAdd(hash6, *(b + 5));
                        hash7 = MultiplyBy2147483647AndAdd(hash7, *(b + 6));
                        hash8 = MultiplyBy2147483647AndAdd(hash8, *(b + 7));
                        b += step;
                    }

                    hash1 = tail == 0 ? hash1 : MultiplyBy2147483647AndAdd(hash1,
                        GetHashCode128Bit(source.Slice(source.Count - tail, tail)));
                }

            return FinalRehash512Bit(hash1, hash2, hash3, hash4, hash5, hash6, hash7, hash8);
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
                        hash = MultiplyBy2147483647AndAdd(hash, *b);
                        b += step;
                    }
                    return MultiplyBy31AndAdd(hash, *e2);
                }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong MultiplyBy2147483647AndAdd(ulong hash, ulong n)
        {
            //2147483647 is a Mercen prime 2^31-1
            return  (hash << 31) - hash + n;//hash * (2^31-1) + n
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong MultiplyBy31AndAdd(ulong hash, ulong n)
        {
            //std java implementation
            return (hash << 5) - hash + n;//hash * 31 + n
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong FinalRehash512Bit(ulong h1, ulong h2,ulong h3, ulong h4, ulong h5, ulong h6,ulong h7, ulong h8)
        {
            return 19 * h1 + 17 * h2 + 13 * h3 + 11 * h4 + 7 * h5 + 5 * h6 + 3 * h7 + h8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Squash(ulong x)
        {
            return (int)((x >> 1) - (x >> 32) + x);//Higher bits * (2^31-1) + Lower bits
        }
    }
}
