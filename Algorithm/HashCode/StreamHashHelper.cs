using Eocron.Algorithms.ByteArray;
using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.HashCode
{
    public static class StreamHashHelper
    {
        public static long GetHashCode(Stream stream, CancellationToken cancellationToken= default, ArrayPool<byte> pool = null)
        {
            pool = pool ?? ArrayPool<byte>.Shared;
            var cmp = ByteArrayEqualityComparer.Default;
            const int seekCount = 16;
            const int hashPartSize = 8 * 1024;

            var hash = 17L;

            var buffer = pool.Rent(hashPartSize);
            try
            {
                int read;
                var len = stream.Length;
                if (len < seekCount * hashPartSize)
                {
                    unchecked
                    {
                        while ((read = stream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            hash = hash * 31 + cmp.GetHashCode(new ArraySegment<byte>(buffer, 0, read));
                        }
                    }
                }
                else
                {
                    var step = len / seekCount;
                    unchecked
                    {
                        for (var i = 0; i < seekCount; i++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            stream.Seek(i * step, SeekOrigin.Begin);

                            read = stream.Read(buffer, 0, buffer.Length);
                            hash = hash * 31 + cmp.GetHashCode(new ArraySegment<byte>(buffer, 0, read));
                        }

                        if (len > buffer.Length)
                        {
                            stream.Seek(-buffer.Length, SeekOrigin.End);
                            read = stream.Read(buffer, 0, buffer.Length);
                            hash = hash * 31 + cmp.GetHashCode(new ArraySegment<byte>(buffer, 0, read));
                        }
                    }

                }
                return hash;
            }
            finally
            {
                pool.Return(buffer);
            }
        }
        public static async Task<long> GetHashCodeAsync(Stream stream, CancellationToken cancellationToken = default, ArrayPool<byte> pool = null)
        {
            pool = pool ?? ArrayPool<byte>.Shared;
            var cmp = ByteArrayEqualityComparer.Default;
            const int seekCount = 16;
            const int hashPartSize = 8 * 1024;

            var hash = 17L;

            var buffer = pool.Rent(hashPartSize);
            try
            {
                int read;
                var len = stream.Length;
                if (len < seekCount * hashPartSize)
                {
                    unchecked
                    {
                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) != 0)
                        {
                            hash = hash * 31 + cmp.GetHashCode(new ArraySegment<byte>(buffer, 0, read));
                        }
                    }
                }
                else
                {
                    var step = len / seekCount;
                    unchecked
                    {
                        for (var i = 0; i < seekCount; i++)
                        {
                            stream.Seek(i * step, SeekOrigin.Begin);

                            read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                            hash = hash * 31 + cmp.GetHashCode(new ArraySegment<byte>(buffer, 0, read));
                        }

                        if (len > buffer.Length)
                        {
                            stream.Seek(-buffer.Length, SeekOrigin.End);
                            read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                            hash = hash * 31 + cmp.GetHashCode(new ArraySegment<byte>(buffer, 0, read));
                        }
                    }

                }
                return hash;
            }
            finally
            {
                pool.Return(buffer);
            }
        }
    }
}
