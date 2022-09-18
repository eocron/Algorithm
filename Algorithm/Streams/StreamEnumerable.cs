using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.Streams
{
    public sealed class StreamEnumerable : IReadOnlyStream<Memory<byte>>
    {
        private readonly Func<Stream> _innerFactory;
        private readonly MemoryPool<byte> _pool;
        private readonly int _desiredBufferSize;

        public StreamEnumerable(Func<Stream> innerFactory, MemoryPool<byte> pool, int desiredBufferSize)
        {
            _innerFactory = innerFactory;
            _pool = pool;
            _desiredBufferSize = desiredBufferSize;
        }

        public IAsyncEnumerator<Memory<byte>> GetAsyncEnumerator(CancellationToken ct = default)
        {
            return new BinaryReadOnlyStreamWrapperEnumerator(_innerFactory, _pool, _desiredBufferSize, ct);
        }


        public IEnumerator<Memory<byte>> GetEnumerator()
        {
            return new BinaryReadOnlyStreamWrapperEnumerator(_innerFactory, _pool, _desiredBufferSize, default);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private sealed class BinaryReadOnlyStreamWrapperEnumerator : IAsyncEnumerator<Memory<byte>>, IEnumerator<Memory<byte>>
        {
            private readonly Lazy<Stream> _inner;
            private readonly Lazy<IMemoryOwner<byte>> _buffer;
            private readonly CancellationToken _ct;
            private Memory<byte> _current;

            public BinaryReadOnlyStreamWrapperEnumerator(
                Func<Stream> innerFactory, 
                MemoryPool<byte> pool,
                int desiredBufferSize,
                CancellationToken ct)
            {
                _inner = new Lazy<Stream>(innerFactory);
                _buffer = new Lazy<IMemoryOwner<byte>>(()=> pool.Rent(desiredBufferSize));
                _ct = ct;
            }

            public async ValueTask DisposeAsync()
            {
                try
                {
                }
                finally
                {
                    if (_inner.IsValueCreated)
                        await _inner.Value.DisposeAsync().ConfigureAwait(false);
                    if(_buffer.IsValueCreated)
                        _buffer.Value.Dispose();
                }
            }
            public void Dispose()
            {
                try
                {
                }
                finally
                {
                    if (_inner.IsValueCreated)
                        _inner.Value.Dispose();
                    if (_buffer.IsValueCreated)
                        _buffer.Value.Dispose();
                }
            }
            public async ValueTask<bool> MoveNextAsync()
            {
                if (!_inner.Value.CanRead)
                {
                    _current = null;
                    return false;
                }

                var read = await _inner.Value.ReadAsync(_buffer.Value.Memory, _ct).ConfigureAwait(false);
                if (read <= 0)
                {
                    _current = null;
                    return false;
                }

                _current = _buffer.Value.Memory.Slice(0, read);
                return true;
            }

            public bool MoveNext()
            {
                if (!_inner.Value.CanRead)
                {
                    _current = null;
                    return false;
                }

                var read = _inner.Value.Read(_buffer.Value.Memory.Span);
                if (read <= 0)
                {
                    _current = null;
                    return false;
                }

                _current = _buffer.Value.Memory.Slice(0, read);
                return true;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            object IEnumerator.Current => Current;

            public Memory<byte> Current => _current;

        }
    }
}