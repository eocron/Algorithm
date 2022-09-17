using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.Streams
{
    public sealed class BinaryWriteOnlyStreamWrapper : IWriteOnlyStream<Memory<byte>>
    {
        private readonly Lazy<Stream> _inner;

        public BinaryWriteOnlyStreamWrapper(Func<Stream> innerFactory)
        {
            _inner = new Lazy<Stream>(innerFactory);
        }
        public async Task WriteAsync(Memory<byte> chunk, CancellationToken ct = default)
        {
            await _inner.Value.WriteAsync(chunk, ct).ConfigureAwait(false);
        }

        public void Write(Memory<byte> chunk)
        {
            _inner.Value.Write(chunk.Span);
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
    }
}