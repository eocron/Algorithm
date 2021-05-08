using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Eocron.Algorithms
{
    /// <summary>
    /// Checks file equality. Possible cancellation and pool usage for internal implementation.
    /// </summary>
    public sealed class FileContentEqualityComparer : IEqualityComparer<string>, IEqualityComparer<FileInfo>
    {
        public static readonly FileContentEqualityComparer Default = new FileContentEqualityComparer();

        private readonly ArrayPool<byte> _pool;
        private readonly IEqualityComparer<ArraySegment<byte>> _byteArrayEqualityComparer;
        private readonly CancellationToken _shutdownToken;
        private readonly int _bufferSize;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="pool">Array pool to use for comparisons.</param>
        /// <param name="bufferSize">Array pool buffer size.</param>
        /// <param name="byteArrayEqualityComparer">Comparer for array pool buffers.</param>
        /// <param name="shutdownToken">Token to cancel all comparison, which can take long time.</param>
        public FileContentEqualityComparer(
            ArrayPool<byte> pool = null, 
            int bufferSize = 8 * 1024, 
            IEqualityComparer<ArraySegment<byte>> byteArrayEqualityComparer = null,
            CancellationToken shutdownToken = default)
        {
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "Invalid buffer size.");
            _pool = pool ?? ArrayPool<byte>.Shared;
            _bufferSize = bufferSize;
            _byteArrayEqualityComparer = byteArrayEqualityComparer ?? ByteArrayEqualityComparer.Default;
            _shutdownToken = shutdownToken;
        }
        public bool Equals(string x, string y)
        {
            return Equals(new FileInfo(x), new FileInfo(y));
        }

        public bool Equals(FileInfo x, FileInfo y)
        {
            if (x.Length != y.Length)
                return false;
            if (x.Length == 0)
                return true;

            using var fs1 = x.OpenRead();
            using var fs2 = y.OpenRead();

            var one = _pool.Rent(_bufferSize);
            try
            {
                var two = _pool.Rent(_bufferSize);
                try
                {
                    var iterCount = x.Length / _bufferSize + (x.Length % _bufferSize == 0 ? 0 : 1);
                    for (int i = 0; i < iterCount; i++)
                    {
                        _shutdownToken.ThrowIfCancellationRequested();
                        var read1 = fs1.Read(one, 0, _bufferSize);
                        var read2 = fs2.Read(two, 0, _bufferSize);

                        if (!_byteArrayEqualityComparer.Equals(
                            new ArraySegment<byte>(one, 0, read1),
                            new ArraySegment<byte>(two, 0, read2)))
                            return false;
                    }
                }
                finally
                {
                    _pool.Return(two);
                }
            }
            finally
            {
                _pool.Return(one);
            }
            return true;
        }

        public int GetHashCode(string obj)
        {
            return GetHashCode(new FileInfo(obj));
        }

        public int GetHashCode(FileInfo obj)
        {
            using var fs = obj.OpenRead();
            return fs.GetHashCode(_shutdownToken, _pool);
        }
    }
}
