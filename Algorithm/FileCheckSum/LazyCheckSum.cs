using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Algorithm.FileCheckSum
{
    /// <summary>
    /// Performs lazy calculation of stream total checksum by splitting stream on parts and calculating hash for each part.
    /// This class usefull for lazy file comparison where you can check only first check sums to identify difference.
    /// Identical files still needs to be fully scanned.
    /// </summary>
    public class LazyCheckSum<T> : ILazyCheckSum<T>
    {
        private readonly Func<Stream> _streamProvider;
        private readonly ICheckSum<T> _checkSumProvider;
        private readonly ArrayPool<byte> _arrayPool;
        private readonly int _bufferSize;
        private readonly bool _disposeStream;
        private List<T> _hashes;
        private long _offset;
        private bool _isEos;

        public LazyCheckSum(
            Func<Stream> streamProvider, 
            ICheckSum<T> checkSumProvider, 
            ArrayPool<byte> arrayPool = null, 
            int bufferSize = 8 * 1024,
            bool disposeStream = true)
        {
            if (streamProvider == null)
                throw new ArgumentNullException(nameof(streamProvider));
            if (checkSumProvider == null)
                throw new ArgumentNullException(nameof(checkSumProvider));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            _streamProvider = streamProvider;
            _checkSumProvider = checkSumProvider;
            _arrayPool = arrayPool;
            _bufferSize = bufferSize;
            _disposeStream = disposeStream;
        }


        public IEnumerator<T> GetEnumerator()
        {
            if (_hashes != null)
            {
                foreach (var p in _hashes)
                    yield return p;
                if (_isEos)
                    yield break;
            }

            var pool = _arrayPool ?? ArrayPool<byte>.Shared;
            var buffer = pool.Rent(_bufferSize);
            try
            {
                var fi = _streamProvider();
                try
                {
                    if (_hashes == null)
                    {
                        var slength = GetStreamLengthSafe(fi) ?? 0;
                        _hashes = new List<T>(_checkSumProvider.CalculateCapacity(slength));
                    }

                    fi.Seek(_offset, SeekOrigin.Begin);
                    while (true)
                    {
                        T hash;
                        var partSize = _checkSumProvider.CalculatePartSize(_hashes);
                        var read = ReadHash(fi, buffer, partSize, out hash);
                        if (read == 0)
                        {
                            _isEos = true;
                            break;
                        }

                        _offset += read;
                        _hashes.Add(hash);
                        yield return hash;
                    }
                }
                finally
                {
                    if (_disposeStream)
                        fi.Dispose();
                }
            }
            finally
            {
                pool.Return(buffer);
            }
        }

        private long? GetStreamLengthSafe(Stream stream)
        {
            try
            {
                return stream.Length;
            }
            catch
            {
                return null;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private int ReadHash(Stream stream, byte[] buffer, int partSize, out T hash)
        {
            hash = _checkSumProvider.InitialHash();
            int toRead = partSize;
            int totalRead = 0;
            while (toRead > 0)
            {
                var read = stream.Read(buffer, 0, Math.Min(buffer.Length, toRead));
                if (read == 0)
                    return totalRead;
                totalRead += read;
                toRead -= read;
                hash = _checkSumProvider.NextHash(hash, buffer, 0, read);
            }
            return totalRead;
        }
    }
}
