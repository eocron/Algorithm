using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Eocron.Sharding.Processing;
using Microsoft.Extensions.Logging;

namespace Eocron.Sharding.Jobs
{
    public sealed class ShardLifetimeJob : IJob, IShardLifetimeManager
    {
        private readonly IJob _inner;
        private readonly ILogger _logger;
        private readonly bool _startOnRun;
        private readonly Channel<CancellationTokenSource> _stopChannel;
        private readonly Channel<object> _startChannel;
        public ShardLifetimeJob(IJob inner, ILogger logger, bool startOnRun)
        {
            _inner = inner;
            _logger = logger;
            _startOnRun = startOnRun;
            _startChannel = Channel.CreateBounded<object>(new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
            _stopChannel = Channel.CreateBounded<CancellationTokenSource>(new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
            ResetAsync().Wait();
        }

        public async Task StopAsync(CancellationToken ct)
        {
            _startChannel.Reader.TryRead(out var _);
            var cts = await _stopChannel.Reader.ReadAsync(ct).ConfigureAwait(false);
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public bool TryStop()
        {
            if (_stopChannel.Reader.TryRead(out var cts))
            {
                try
                {
                    cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
                return true;
            }
            return false;
        }

        public async Task StartAsync(CancellationToken ct)
        {
            if (IsStopped())
            {
                await _startChannel.Writer.WriteAsync(new object(), ct).ConfigureAwait(false);
            }
        }

        public bool IsStopped()
        {
            return !_stopChannel.Reader.TryPeek(out var _);
        }

        public async Task RestartAsync(CancellationToken ct)
        {
            TryStop();
            await _startChannel.Writer.WriteAsync(new object(), CancellationToken.None).ConfigureAwait(false);
        }

        public async Task RunAsync(CancellationToken ct)
        {
            await Task.Yield();
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                {
                    await _startChannel.Reader.ReadAsync(ct).ConfigureAwait(false);
                    await _stopChannel.Writer.WriteAsync(cts, ct).ConfigureAwait(false);

                    _logger.LogInformation("Job starting");
                    try
                    {
                        await _inner.RunAsync(cts.Token).ConfigureAwait(false);
                        await ResetAsync();
                    }
                    catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
                    {
                        _logger.LogInformation("Job stopped");
                    }
                    catch (Exception)
                    {
                        await ResetAsync();
                        throw;
                    }
                }
            }
        }

        private async Task ResetAsync()
        {
            if (_startOnRun)
            {
                await _startChannel.Writer.WriteAsync(new object(), CancellationToken.None)
                    .ConfigureAwait(false);
            }
            _stopChannel.Reader.TryRead(out var _);
        }

        public void Dispose()
        {
            _inner.Dispose();
        }
    }
}