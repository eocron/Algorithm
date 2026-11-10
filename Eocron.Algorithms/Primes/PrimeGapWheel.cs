using System.Collections.Generic;
using System.Numerics;

namespace Eocron.Algorithms.Primes
{
    public sealed class PrimeGapWheel
    {
        public long Period { get; }
        public long Count { get; }

        public PrimeGapWheel()
        {
            Period = 1;
            _bits = [0b1];
            Count = 1;
        }
        
        private PrimeGapWheel(long period, ulong[] bits, long count)
        {
            Period = period;
            _bits = bits;
            Count = count;
        }

        public bool IsCoprime(long x) => Get(_bits, x % Period);

        public IEnumerable<int> Gaps()
        {
            if (Period == 1)
            {
                yield return 1;
                yield break;
            }

            long prev = -1;
            foreach (var x in Survivors())
            {
                if (prev >= 0) yield return (int)(x - prev);
                prev = x;
            }

            yield return (int)(Period + 1 - prev); 
        }

        public long NextPrime()
        {
            foreach (var x in Survivors())
                if (x > 1)
                    return x;
            return Period + 1;
        }

        public PrimeGapWheel CreateNextWheel()
        {
            var p = NextPrime();
            var newPeriod = checked(Period * p);
            var nb = new ulong[(newPeriod + 63) >> 6];

            foreach (var r in Survivors())
                for (long k = 0; k < p; k++)
                    Set(nb, k * Period + r);

            foreach (var w in Survivors())
                Clear(nb, p * w);
            
            return new PrimeGapWheel(newPeriod, nb, Count * (p - 1));
        }
        
        private IEnumerable<long> Survivors()
        {
            for (var w = 0; w < _bits.Length; w++)
            {
                var v = _bits[w];
                while (v != 0)
                {
                    var t = BitOperations.TrailingZeroCount(v);
                    yield return ((long)w << 6) + t;
                    v &= v - 1;
                }
            }
        }
        
        private static bool Get(ulong[] b, long i) => (b[i >> 6] >> (int)(i & 63) & 1) != 0;
        private static void Set(ulong[] b, long i) => b[i >> 6] |= 1UL << (int)(i & 63);
        private static void Clear(ulong[] b, long i) => b[i >> 6] &= ~(1UL << (int)(i & 63));
        
        private readonly ulong[] _bits;
    }
}