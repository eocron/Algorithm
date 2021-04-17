using Eocron.Algorithms.ByteArray;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.FileCheckSum
{
    public static class StreamHashHelper
    {
        private static readonly IEqualityComparer<ArraySegment<byte>> _cmp = new ByteArrayEqualityComparer();
        public static async Task<long> GetHashCodeAsync(Stream stream, CancellationToken cancellationToken = default, ArrayPool<byte> pool = null)
        {
            pool = pool ?? ArrayPool<byte>.Shared;
            var hash = 17L;
            const int seekCount = 16;
            const int hashPartSize = 8*1024;
            var buffer = pool.Rent(hashPartSize);
            try
            {
                int read;
                var len = stream.Length;
                if (len < seekCount * hashPartSize)
                {
                    unchecked
                    {
                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            hash = hash * 31 + _cmp.GetHashCode(new ArraySegment<byte>(buffer, 0, read));
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
                            hash = hash * 31 + _cmp.GetHashCode(new ArraySegment<byte>(buffer, 0, read));
                        }

                        if (len > buffer.Length)
                        {
                            stream.Seek(-buffer.Length, SeekOrigin.End);
                            read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                            hash = hash * 31 + _cmp.GetHashCode(new ArraySegment<byte>(buffer, 0, read));
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
