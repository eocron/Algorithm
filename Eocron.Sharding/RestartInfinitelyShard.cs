using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Eocron.Sharding
{
    public sealed class RestartInfinitelyShard<TInput, TOutput, TError> : IShard<TInput, TOutput, TError>
    {
        private readonly ILogger _logger;
        private readonly TimeSpan _onErrorRestartInterval;
        private readonly IShard<TInput, TOutput, TError> _inner;
        private readonly TimeSpan _onCompleteRestartInterval;

        public RestartInfinitelyShard(IShard<TInput, TOutput, TError> inner, ILogger logger, TimeSpan onErrorRestartInterval, TimeSpan onCompleteRestartInterval)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _onErrorRestartInterval = onErrorRestartInterval;
            _onCompleteRestartInterval = onCompleteRestartInterval;
        }

        public bool IsReadyForPublish()
        {
            return _inner.IsReadyForPublish();
        }

        public ChannelReader<TOutput> Outputs => _inner.Outputs;

        public ChannelReader<TError> Errors => _inner.Errors;

        public Task PublishAsync(IEnumerable<TInput> messages, CancellationToken ct)
        {
            return _inner.PublishAsync(messages, ct);
        }

        public async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    _logger.LogInformation("Shard starting");
                    await _inner.RunAsync(ct).ConfigureAwait(false);
                    _logger.LogInformation("Shard stopped without error, running for {elapsed}", sw.Elapsed);
                    await Task.Delay(_onCompleteRestartInterval, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Shard stopped with error, running for {elapsed}", sw.Elapsed);
                    try
                    {
                        await Task.Delay(_onErrorRestartInterval, ct).ConfigureAwait(false);
                    }
                    catch
                    {
                        break;
                    }
                }
            }
        }

        public void Dispose()
        {
            _inner.Dispose();
        }
    }
}