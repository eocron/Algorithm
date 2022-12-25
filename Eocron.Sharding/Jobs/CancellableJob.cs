using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Eocron.Sharding.Jobs
{
    public sealed class CancellableJob : IJob, ICancellationManager
    {
        private readonly IJob _inner;
        private readonly Channel<CancellationTokenSource> _ctsChannel;
        public CancellableJob(IJob inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _ctsChannel = Channel.CreateBounded<CancellationTokenSource>(
                new BoundedChannelOptions(1)
                {
                    FullMode = BoundedChannelFullMode.DropOldest
                });
        }

        public async Task CancelAsync(CancellationToken ct)
        {
            var cts = await _ctsChannel.Reader.ReadAsync(ct).ConfigureAwait(false);
            cts.Cancel();
        }

        public bool TryCancel()
        {
            if (_ctsChannel.Reader.TryRead(out var cts))
            {
                cts.Cancel();
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public async Task RunAsync(CancellationToken ct)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            {
                await _ctsChannel.Writer.WriteAsync(cts, ct).ConfigureAwait(false);
                await _inner.RunAsync(cts.Token).ConfigureAwait(false);
            }
        }
    }
}