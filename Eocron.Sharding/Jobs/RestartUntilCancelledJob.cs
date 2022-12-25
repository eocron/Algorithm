using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Eocron.Sharding.Jobs
{
    public sealed class RestartUntilCancelledJob : IJob
    {
        public RestartUntilCancelledJob(IJob inner, ILogger logger, TimeSpan onErrorRestartInterval,
            TimeSpan onSuccessRestartInterval)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _onErrorRestartInterval = onErrorRestartInterval;
            _onSuccessRestartInterval = onSuccessRestartInterval;
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public async Task RunAsync(CancellationToken ct)
        {
            await Task.Yield();
            while (!ct.IsCancellationRequested)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    _logger.LogInformation("Job starting");
                    await _inner.RunAsync(ct).ConfigureAwait(false);
                    _logger.LogInformation("Job completed, running for {elapsed}", sw.Elapsed);
                    await Task.Delay(_onSuccessRestartInterval, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Job completed with error, running for {elapsed}", sw.Elapsed);
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

        private readonly IJob _inner;
        private readonly ILogger _logger;
        private readonly TimeSpan _onErrorRestartInterval;
        private readonly TimeSpan _onSuccessRestartInterval;
    }
}