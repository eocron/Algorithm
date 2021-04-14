using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace Algorithm.FileCheckSum
{
    /// <summary>
    /// Performs lazy calculation of stream total checksum by splitting stream on equal size parts and calculating hash for each part.
    /// This class usefull for lazy file comparison where you can check only first check sums to identify difference.
    /// Identical files still needs to be fully scanned.
    /// </summary>
    public class PartitionedLazyCheckSum : ILazyCheckSum<int>
    {
        private readonly Func<Stream> _streamProvider;
        private readonly ArrayPool<byte> _arrayPool;
        private readonly int _bufferSize;
        private readonly bool _disposeStream;
        private readonly int _partSize;
        private List<int> _hashes;
        private long _offset;


        public PartitionedLazyCheckSum(
            Func<Stream> streamProvider, 
            int partSize, 
            ArrayPool<byte> arrayPool = null, 
            int bufferSize = 8 * 1024,
            bool disposeStream = true)
        {
            if (partSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(partSize));
            if (streamProvider == null)
                throw new ArgumentNullException(nameof(streamProvider));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            _partSize = partSize;
            _streamProvider = streamProvider;
            _arrayPool = arrayPool;
            _bufferSize = bufferSize;
            _disposeStream = disposeStream;
        }


        public IEnumerator<int> GetEnumerator()
        {
            if (_hashes != null)
            {
                foreach (var p in _hashes)
                    yield return p;
            }

            if (_hashes != null && _hashes.Count * _partSize >= _offset)
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
                            _hashes = new List<int>((int)(slength / _partSize) + ((slength % _partSize) > 0 ? 1 : 0));
                        }

                        fi.Seek(_offset, SeekOrigin.Begin);
                        while (true)
                        {
                            int hash;
                            var read = ReadHash(fi, buffer, out hash);
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

        private int ReadHash(Stream stream, byte[] buffer, out int hash)
        {
            hash = 17;
            int toRead = _partSize;
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
