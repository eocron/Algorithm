using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;

namespace Eocron.Sharding
{
    public sealed class RestartUntilFinishedShard<TInput, TOutput, TError> : IShard<TInput, TOutput, TError>
    {
        private readonly ILogger _logger;
        private readonly TimeSpan _restartInterval;
        private readonly IShard<TInput, TOutput, TError> _inner;

        public RestartUntilFinishedShard(IShard<TInput, TOutput, TError> inner, ILogger logger, TimeSpan restartInterval)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _restartInterval = restartInterval;
        }

        public bool IsReadyForPublish()
        {
            return _inner.IsReadyForPublish();
        }

        public IReceivableSourceBlock<TOutput> Outputs => _inner.Outputs;

        public IReceivableSourceBlock<TError> Errors => _inner.Errors;

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
                    break;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Shard stopped with error, running for {elapsed}", sw.Elapsed);
                }

                try
                {
                    await Task.Delay(_restartInterval, ct).ConfigureAwait(false);
                }
                catch
                {
                    break;
                }
            }
        }

        public void Dispose()
        {
            _inner.Dispose();
        }
    }
}