using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Eocron.Algorithms.Randoms
{
    public static class RandomExtensions
    {
        /// <summary>
        ///     Retrieves any random element from collection.
        /// </summary>
        /// <typeparam name="T">Element type</typeparam>
        /// <param name="random">The given random instance</param>
        /// <param name="collection">Possible domain of values</param>
        /// <returns></returns>
        public static T NextAny<T>(this Random random, params T[] collection)
        {
            if (collection == null || collection.Length == 0)
                throw new ArgumentNullException(nameof(collection));
            return collection[random.Next(0, collection.Length)];
        }

        /// <summary>
        ///     Retrieves any random element from collection.
        /// </summary>
        /// <typeparam name="T">Element type</typeparam>
        /// <param name="random">The given random instance</param>
        /// <param name="collection">Possible domain of values</param>
        /// <returns></returns>
        public static T NextAny<T>(this Random random, IList<T> collection)
        {
            if (collection == null || collection.Count == 0)
                throw new ArgumentNullException(nameof(collection));
            return collection[random.Next(0, collection.Count)];
        }

        /// <summary>
        ///     Returns random boolean.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public static bool NextBool(this Random random)
        {
            const int half = int.MaxValue >> 1;
            return random.Next() > half;
        }

        /// <summary>
        ///     Creates file with random content or appends to existing.
        /// </summary>
        /// <param name="rnd">The given random instance</param>
        /// <param name="size">File size</param>
        public static void NextFile(this Random rnd, string filePath, long size, ArrayPool<byte> pool = null)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));
            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(size), size, "Invalid random file size.");
            using var rs = rnd.NextStream(size, pool: pool);
            using var fs = File.OpenWrite(filePath);
            rs.CopyTo(fs);
        }

        /// <summary>
        ///     Creates random guid. Usefull when you want stable guid generation binded to Random seed.
        /// </summary>
        /// <param name="random">The given random instance</param>
        /// <returns></returns>
        public static Guid NextGuid(this Random random)
        {
            var guid = new byte[16];
            random.NextBytes(guid);
            return new Guid(guid);
        }

        /// <summary>
        ///     Returns a random long from min (inclusive) to max (exclusive)
        /// </summary>
        /// <param name="random">The given random instance</param>
        /// <param name="min">The inclusive minimum bound</param>
        /// <param name="max">The exclusive maximum bound.  Must be greater than min</param>
        public static long NextLong(this Random random, long min, long max, ArrayPool<byte> pool = null)
        {
            if (max <= min)
                throw new ArgumentOutOfRangeException("max", "max must be > min!");
            pool = pool ?? ArrayPool<byte>.Shared;
            //Working with ulong so that modulo works correctly with values > long.MaxValue
            var uRange = (ulong)(max - min);

            //Prevent a modolo bias; see https://stackoverflow.com/a/10984975/238419
            //for more information.
            //In the worst case, the expected number of calls is 2 (though usually it's
            //much closer to 1) so this loop doesn't really hurt performance at all.
            ulong ulongRand;
            var buf = pool.Rent(sizeof(ulong));
            try
            {
                do
                {
                    random.NextBytes(buf);
                    ulongRand = (ulong)BitConverter.ToInt64(buf, 0);
                } while (ulongRand > ulong.MaxValue - (ulong.MaxValue % uRange + 1) % uRange);
            }
            finally
            {
                pool.Return(buf);
            }

            return (long)(ulongRand % uRange) + min;
        }

        /// <summary>
        ///     Returns a random long from 0 (inclusive) to max (exclusive)
        /// </summary>
        /// <param name="random">The given random instance</param>
        /// <param name="max">The exclusive maximum bound.  Must be greater than 0</param>
        public static long NextLong(this Random random, long max)
        {
            return random.NextLong(0, max);
        }

        /// <summary>
        ///     Returns a random long over all possible values of long (except long.MaxValue, similar to
        ///     random.Next())
        /// </summary>
        /// <param name="random">The given random instance</param>
        public static long NextLong(this Random random)
        {
            return random.NextLong(long.MinValue, long.MaxValue);
        }

        /// <summary>
        ///     Returns stream of random bytes. Can be used to generate random files or for testing purposes.
        /// </summary>
        /// <param name="rnd"></param>
        /// <param name="size">Stream size</param>
        /// <param name="blockLength">Stream block size</param>
        /// <returns></returns>
        public static Stream NextStream(this Random rnd, long size = long.MaxValue, int blockLength = 8 * 1024,
            ArrayPool<byte> pool = null)
        {
            return new RandomStream(rnd.NextLong(), size, blockLength, pool ?? ArrayPool<byte>.Shared);
        }

        /// <summary>
        ///     Returns random string from specified domain of characters.
        /// </summary>
        /// <param name="random"></param>
        /// <param name="size"></param>
        /// <param name="domain">Default domain is '0123456789abcdef'</param>
        /// <returns></returns>
        public static string NextString(this Random random, int size, params char[] domain)
        {
            if (domain == null || domain.Length == 0) domain = DefaultStringDomain;
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
        ///     Shuffles elements of incoming sequence
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="random">The given random instance</param>
        /// <param name="enumerable">Incoming sequence</param>
        /// <returns></returns>
        public static IEnumerable<T> Shuffle<T>(this Random random, IEnumerable<T> enumerable)
        {
            return enumerable
                .Select(x => new { Number = random.Next(), Item = x })
                .OrderBy(x => x.Number)
                .Select(x => x.Item);
        }

        public static readonly char[] DefaultStringDomain = "0123456789abcdef".ToCharArray();
    }
}