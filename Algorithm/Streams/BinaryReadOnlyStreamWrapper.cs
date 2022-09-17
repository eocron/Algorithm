using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.Streams
{
    public sealed class BinaryReadOnlyStreamWrapper : IReadOnlyStream<Memory<byte>>
    {
        private readonly Func<Stream> _innerFactory;
        private readonly Func<Memory<byte>> _bufferProvider;

        public BinaryReadOnlyStreamWrapper(Func<Stream> innerFactory, Func<Memory<byte>> bufferProvider)
        {
            _innerFactory = innerFactory;
            _bufferProvider = bufferProvider;
        }

        public IAsyncEnumerator<Memory<byte>> GetAsyncEnumerator(CancellationToken ct = default)
        {
            return new BinaryReadOnlyStreamWrapperEnumerator(_innerFactory, _bufferProvider, ct);
        }


        public IEnumerator<Memory<byte>> GetEnumerator()
        {
            return new BinaryReadOnlyStreamWrapperEnumerator(_innerFactory, _bufferProvider, default);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private sealed class BinaryReadOnlyStreamWrapperEnumerator : IAsyncEnumerator<Memory<byte>>, IEnumerator<Memory<byte>>
        {
            private readonly Lazy<Stream> _inner;
            private readonly Lazy<Memory<byte>> _buffer;
            private readonly CancellationToken _ct;
            private Memory<byte> _current;

            public BinaryReadOnlyStreamWrapperEnumerator(Func<Stream> innerFactory, Func<Memory<byte>> bufferProvider,
                CancellationToken ct)
            {
                _inner = new Lazy<Stream>(innerFactory);
                _buffer = new Lazy<Memory<byte>>(bufferProvider);
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
                }
            }
            public async ValueTask<bool> MoveNextAsync()
            {
                if (!_inner.Value.CanRead)
                {
                    _current = null;
                    return false;
                }

                var read = await _inner.Value.ReadAsync(_buffer.Value, _ct).ConfigureAwait(false);
                if (read <= 0)
                {
                    _current = null;
                    return false;
                }

                _current = _buffer.Value.Slice(0, read);
                return true;
            }

            public bool MoveNext()
            {
                if (!_inner.Value.CanRead)
                {
                    _current = null;
                    return false;
                }

                var read = _inner.Value.Read(_buffer.Value.Span);
                if (read <= 0)
                {
                    _current = null;
                    return false;
                }

                _current = _buffer.Value.Slice(0, read);
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