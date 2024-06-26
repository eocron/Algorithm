using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Eocron.ProxyHost;

public sealed class ProxyHandler : BackgroundService
{
    private readonly IProxyUpStreamConnectionProducer _producer;
    private readonly IConnectionWatcher _watcher;
    private readonly ILogger _logger;

    public ProxyHandler(IProxyUpStreamConnectionProducer producer, IConnectionWatcher watcher, ILogger logger)
    {
        _producer = producer;
        _watcher = watcher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await foreach (var pendingConnection in _producer.GetPendingConnections(stoppingToken))
                {
                    await pendingConnection.StartAsync(stoppingToken).ConfigureAwait(false);
                    _watcher.Watch(pendingConnection);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get pending connections");
            }
        }
    }
}