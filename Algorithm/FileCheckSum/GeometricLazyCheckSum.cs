using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Algorithm.FileCheckSum
{
    /// <summary>
    /// Performs lazy calculation of stream total checksum by splitting stream on geometric progression size parts and calculating hash for each part.
    /// This class usefull for lazy file comparison where you can check only first check sums to identify difference.
    /// Identical files still needs to be fully scanned.
    /// </summary>
    public class GeometricLazyCheckSum : ILazyCheckSum<int>
    {
        private readonly int _bufferSize;
        private readonly bool _disposeStream;
        private readonly Func<Stream> _streamProvider;
        private readonly ArrayPool<byte> _arrayPool;

        private readonly int _a;
        private readonly int _q;
        private List<int> _hashes;
        private long _offset;


        public GeometricLazyCheckSum(
            Func<Stream> streamProvider, 
            int a, 
            int q, 
            ArrayPool<byte> arrayPool = null, 
            int bufferSize = 8 * 1024, 
            bool disposeStream = true)
        {
            if (a <= 0)
                throw new ArgumentOutOfRangeException(nameof(a));
            if (q <= 1)
                throw new ArgumentOutOfRangeException(nameof(a));
            if (streamProvider == null)
                throw new ArgumentNullException(nameof(streamProvider));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            _streamProvider = streamProvider;
            _arrayPool = arrayPool;
            _a = a;
            _q = q;
            _bufferSize = bufferSize;
            _disposeStream = disposeStream;
        }

        private int GetPartsOffset()
        {
            return _a * (1 - (int)Math.Pow(_q, _hashes.Count)) / (1 - _q);
        }

        public IEnumerator<int> GetEnumerator()
        {
            if (_hashes != null)
            {
                foreach (var p in _hashes)
                    yield return p;
            }

            if (_hashes != null && GetPartsOffset() >= _offset)
            {
                yield break;
            }
            else
            {
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
                            var capacity = (int)Math.Ceiling(Math.Log((slength / _a) * (_q - 1) + 1, _q));
                            _hashes = new List<int>(capacity);
                        }

                        fi.Seek(_offset, SeekOrigin.Begin);
                        while (true)
                        {
                            int hash;
                            var read = ReadHash(fi, buffer, (int)(_a * Math.Pow(_q, _hashes.Count)), out hash);
                            if (read == 0)
                                break;
                            
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

        private int ReadHash(Stream stream, byte[] buffer, int partSize, out int hash)
        {
            hash = 17;
            int toRead = partSize;
            int totalRead = 0;
            while (toRead > 0)
            {
                var read = stream.Read(buffer, 0, Math.Min(buffer.Length, toRead));
                if (read == 0)
                    return totalRead;
                totalRead += read;
                toRead -= read;
                unchecked
                {
                    for (var i = 0; i < read; i++)
                    {
                        hash = hash * 31 + buffer[i];
                    }
                }
            }
            return totalRead;
        }
    }
}
