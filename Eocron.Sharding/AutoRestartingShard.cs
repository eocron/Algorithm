using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Eocron.Sharding;

public sealed class AutoRestartingShard<TInput, TOutput, TError> : IShard<TInput, TOutput, TError>
{
    private readonly ILogger _logger;
    private readonly TimeSpan _restartInterval;
    private readonly IShard<TInput, TOutput, TError> _inner;

    public AutoRestartingShard(IShard<TInput, TOutput, TError> inner, ILogger logger, TimeSpan restartInterval)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger;
        _restartInterval = restartInterval;
    }
    
    public IAsyncEnumerable<TOutput> GetOutputEnumerable(CancellationToken ct)
    {
        return _inner.GetOutputEnumerable(ct);
    }

    public IAsyncEnumerable<TError> GetErrorsEnumerable(CancellationToken ct)
    {
        return _inner.GetErrorsEnumerable(ct);
    }

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
}