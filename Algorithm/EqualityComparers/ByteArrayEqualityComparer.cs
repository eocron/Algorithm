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
        private const ulong HashSeed = 0x9e3779b97f4a7c15UL;
        private static readonly UInt128[] UInt128Masks = Enumerable.Range(0, 2 * sizeof(ulong)).Select(x => CreateMask(x * sizeof(ulong))).ToArray();
        private readonly bool _hashWithLoss;
        private const int _hashLossPow = 10;
        private const int _hashLoss = (1 << _hashLossPow) * sizeof(long);
        
        public static readonly ByteArrayEqualityComparer Default = new ByteArrayEqualityComparer();
        
        public ByteArrayEqualityComparer(bool hashWithLoss = false)
        {
            _hashWithLoss = hashWithLoss;
        }

        public int GetHashCode(ArraySegment<byte> obj)
        {
            if (obj == null)
                return 0;
            if (obj.Count == 0)
                return 1;
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

                return Equals128BitTail(*b1, *(b1+1), *b2, *(b2+1), tail);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equals128BitTail(ulong a1, ulong a2, ulong b1, ulong b2, int byteCount)
        {
            return (a1 & UInt128Masks[byteCount].Low) == (b1 & UInt128Masks[byteCount].Low) &&
                   (a2 & UInt128Masks[byteCount].High) == (b2 & UInt128Masks[byteCount].High);
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
            var hash1 = HashSeed;
            var hash2 = HashSeed;
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
                        hash1 = Rehash128Bit(hash1, *b);
                        hash2 = Rehash128Bit(hash2, *(b + 1));
                        b += step;
                    }

                    hash1 = Rehash128Bit(hash1, *b & UInt128Masks[tail].Low);
                    hash2 = Rehash128Bit(hash2, *(b + 1) & UInt128Masks[tail].High);
                }

            return Rehash128Bit(hash1, hash2);
        }

        private static unsafe ulong GetHashCode512Bit(ArraySegment<byte> source)
        {
            ulong hash1 = Rehash128Bit(HashSeed, (ulong)source.Count);
            ulong hash2 = HashSeed;
            ulong hash3 = HashSeed;
            ulong hash4 = HashSeed;
            ulong hash5 = HashSeed;
            ulong hash6 = HashSeed;
            ulong hash7 = HashSeed;
            ulong hash8 = HashSeed;
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
                        hash1 = Rehash128Bit(hash1, *b);
                        hash2 = Rehash128Bit(hash2, *(b + 1));
                        hash3 = Rehash128Bit(hash3, *(b + 2));
                        hash4 = Rehash128Bit(hash4, *(b + 3));
                        hash5 = Rehash128Bit(hash5, *(b + 4));
                        hash6 = Rehash128Bit(hash6, *(b + 5));
                        hash7 = Rehash128Bit(hash7, *(b + 6));
                        hash8 = Rehash128Bit(hash8, *(b + 7));
                        b += step;
                    }

                    hash1 = tail == 0
                        ? hash1
                        : Rehash128Bit(
                            hash1,
                            GetHashCode128Bit(
                                source.Slice(
                                    source.Count - tail,
                                    tail)));
                }

            return Rehash512Bit(hash1, hash2, hash3, hash4, hash5, hash6, hash7, hash8);
        }

        private static unsafe ulong GetHashCodeLoss(ArraySegment<byte> source)
        {
            var hash = HashSeed;
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
                        hash = Rehash128Bit(hash, *b);
                        b += step;
                    }
                    return Rehash128Bit(hash, *e2);
                }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Rehash128Bit(ulong z, ulong n)
        {
            return (z << 5) - z + n;
            //return XorShift(XorShift(XorShift(z + 0x9e3779b97f4a7c15UL, 30) + n + 0xbf58476d1ce4e5b9UL, 27) * 0x94d049bb133111ebUL, 31);
            //return XorShift(XorShiftL(z, 7) ^ n, 13);
            //return XorShift(XorShift(z, 13) ^ n, 13) * 0x94d049bb133111ebUL;
        }
        
        /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong XorShift(ulong z, int n)
        {
            return z ^ (z >> n);
        }*/
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Rehash512Bit(ulong h1, ulong h2,ulong h3, ulong h4, ulong h5, ulong h6,ulong h7, ulong h8)
        {
            return Rehash128Bit(Rehash128Bit(Rehash128Bit(h1, h2), Rehash128Bit(h3, h4)), Rehash128Bit(Rehash128Bit(h5, h6), Rehash128Bit(h7, h8)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Squash(ulong x)
        {
            return x.GetHashCode();
        }
        
        private readonly struct UInt128
        {
            public readonly ulong High;
            public readonly ulong Low;
            public UInt128(ulong high, ulong low)
            {
                High = high;
                Low = low;
            }
        }
        
        private static UInt128 CreateMask(int byteCount)
        {
            if (byteCount == 0)
                return new UInt128();
            return new UInt128(
                byteCount > sizeof(ulong) ? ulong.MaxValue >> (8 * 2 * sizeof(ulong) - byteCount) : 0,
                byteCount >= sizeof(ulong) ? ulong.MaxValue : ulong.MaxValue >> (8 * (sizeof(ulong) - byteCount)));
        }
    }
}
