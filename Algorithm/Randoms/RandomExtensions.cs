using System;
using System.Buffers;
using System.IO;

namespace Eocron.Algorithms
{

    public static class RandomExtensions
    {
        public static readonly char[] DefaultStringDomain = "0123456789abcdef".ToCharArray();
        public static ArrayPool<byte> DefaultArrayPool = ArrayPool<byte>.Shared;
        /// <summary>
        /// Returns random string from specified domain of characters.
        /// </summary>
        /// <param name="random"></param>
        /// <param name="size"></param>
        /// <param name="domain">Default domain is '0123456789abcdef'</param>
        /// <returns></returns>
        public static string NextString(this Random random, int size, params char[] domain)
        {
            domain = domain ?? DefaultStringDomain;
            if (domain == null || domain.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(domain));
            if (size == 0)
                throw new ArgumentOutOfRangeException(nameof(size));

            var sb = new char[size];
            for (var i = 0; i < size; i++)
                sb[i] = domain[random.Next(0, domain.Length)];
            return new string(sb);
        }

        /// <summary>
        /// Returns random boolean.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public static bool NextBool(this Random random)
        {
            const int half = int.MaxValue >> 1;
            return random.Next() > half;
        }

        /// <summary>
        /// Returns a random long from min (inclusive) to max (exclusive)
        /// </summary>
        /// <param name="random">The given random instance</param>
        /// <param name="min">The inclusive minimum bound</param>
        /// <param name="max">The exclusive maximum bound.  Must be greater than min</param>
        public static long NextLong(this Random random, long min, long max)
        {
            if (max <= min)
                throw new ArgumentOutOfRangeException("max", "max must be > min!");

            //Working with ulong so that modulo works correctly with values > long.MaxValue
            ulong uRange = (ulong)(max - min);

            //Prevent a modolo bias; see https://stackoverflow.com/a/10984975/238419
            //for more information.
            //In the worst case, the expected number of calls is 2 (though usually it's
            //much closer to 1) so this loop doesn't really hurt performance at all.
            ulong ulongRand;
            var buf = DefaultArrayPool.Rent(8);
            try
            {
                do
                {
                    random.NextBytes(buf);
                    ulongRand = (ulong)BitConverter.ToInt64(buf, 0);
                } while (ulongRand > ulong.MaxValue - ((ulong.MaxValue % uRange) + 1) % uRange);
            }
            finally
            {
                DefaultArrayPool.Return(buf);
            }
            return (long)(ulongRand % uRange) + min;
        }

        /// <summary>
        /// Returns a random long from 0 (inclusive) to max (exclusive)
        /// </summary>
        /// <param name="random">The given random instance</param>
        /// <param name="max">The exclusive maximum bound.  Must be greater than 0</param>
        public static long NextLong(this Random random, long max)
        {
            return random.NextLong(0, max);
        }

        /// <summary>
        /// Returns a random long over all possible values of long (except long.MaxValue, similar to
        /// random.Next())
        /// </summary>
        /// <param name="random">The given random instance</param>
        public static long NextLong(this Random random)
        {
            return random.NextLong(long.MinValue, long.MaxValue);
        }

        /// <summary>
        /// Returns stream of random bytes. Can be used to generate random files or for testing purposes.
        /// </summary>
        /// <param name="rnd"></param>
        /// <param name="size"></param>
        /// <param name="blockLength"></param>
        /// <returns></returns>
        public static Stream NextStream(this Random rnd, long size = long.MaxValue, int blockLength = 8*1024)
        {
            return new RandomStream(rnd.NextLong(), size, blockLength, DefaultArrayPool);
        }
    }
}
