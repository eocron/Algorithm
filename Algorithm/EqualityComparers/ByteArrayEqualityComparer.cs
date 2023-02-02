using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Eocron.Algorithms.EqualityComparers.xxHash;

namespace Eocron.Algorithms
{
    /// <summary>
    /// Extreme version of byte array equality comparer for x64 environments. 
    /// Perfromance is just like memcpm.
    /// </summary>
    public sealed class ByteArrayEqualityComparer : IEqualityComparer<ArraySegment<byte>>, IEqualityComparer<byte[]>
    {
        private const ulong HashSeed = 0x9e3779b97f4a7c15UL;
        private static readonly UInt128[] UInt128Masks = Enumerable.Range(0, 2 * sizeof(ulong)).Select(CreateMask).ToArray();
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
            return Squash(xxHash64.ComputeHash(obj, HashSeed));
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
            var mask = UInt128Masks[byteCount];
            return (a1 & mask.Low) == (b1 & mask.Low) && (a2 & mask.High) == (b2 & mask.High);
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Squash(ulong x)
        {
            return x.GetHashCode();
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private readonly struct UInt128
        {
            public readonly ulong High;
            public readonly ulong Low;
            public UInt128(ulong high, ulong low)
            {
                High = high;
                Low = low;
            }

            public override string ToString()
            {
                return High.ToString("X") + " " + Low.ToString("X");
            }
        }
        
        private static UInt128 CreateMask(int byteCount)
        {
            if (byteCount == 0)
                return new UInt128();
            return new UInt128(
                byteCount > sizeof(ulong) ? ulong.MaxValue >> (8 * (2 * sizeof(ulong) - byteCount)) : 0,
                byteCount >= sizeof(ulong) ? ulong.MaxValue : ulong.MaxValue >> (8 * (sizeof(ulong) - byteCount)));
        }
    }
}