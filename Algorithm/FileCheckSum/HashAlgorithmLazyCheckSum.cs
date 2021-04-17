using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Eocron.Algorithms.FileCheckSum
{
    public sealed class HashAlgorithmLazyCheckSum : IHashAlgorithmLazyCheckSum
    {
        private readonly int _firstHashSize;
        private readonly ArrayPool<byte> _arrayPool;
        private readonly bool _disposeStream;
        private readonly Func<Stream> _streamProvider;
        private readonly HashAlgorithm _secondHashAlgorithm;
        private readonly bool _dispose;
        private readonly Lazy<byte[]> _firstHash;
        private readonly Lazy<byte[]> _secondHash;

        public HashAlgorithmLazyCheckSum(Func<Stream> streamProvider, HashAlgorithm secondHashAlgorithm, int firstHashSize = 4*1024, ArrayPool<byte> arrayPool = null,
            bool disposeStream = true)
        {
            _firstHashSize = firstHashSize;
            _arrayPool = arrayPool;
            _disposeStream = disposeStream;
            _streamProvider = streamProvider;
            _secondHashAlgorithm = secondHashAlgorithm;
            _firstHash = new Lazy<byte[]>(GetFirstHash);
            _secondHash = new Lazy<byte[]>(GetSecondHash);
        }

        private byte[] GetFirstHash()
        {
            var stream = _streamProvider();
            try
            {
                var pool = _arrayPool ?? ArrayPool<byte>.Shared;
                var buffer = pool.Rent(_firstHashSize);
                try
                {
                    throw new Exception();
                }
                finally
                {
                    pool.Return(buffer);
                }
            }
            finally
            {
                if(_disposeStream)
                    stream.Dispose();
            }
        }

        private byte[] GetSecondHash()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (_dispose)
                _secondHashAlgorithm.Dispose();
        }

        public IEnumerator<byte[]> GetEnumerator()
        {
            yield return _firstHash.Value;
            yield return _secondHash.Value;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
