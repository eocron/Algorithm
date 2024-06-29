using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Eocron.ProxyHost.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Eocron.ProxyHost;

public sealed class ConnectionWatcher : BackgroundService, IConnectionWatcher
{
    private readonly ILogger _logger;
    private readonly TimeSpan _stopTimeout;
    private readonly TimeSpan _gcInterval;
    private readonly ConcurrentList<IProxyConnection> _connections = new();
        
    public ConnectionWatcher(ILogger logger, TimeSpan stopTimeout, TimeSpan gcInterval)
    {
        _logger = logger;
        _stopTimeout = stopTimeout;
        _gcInterval = gcInterval;
    }

    public void Watch(IProxyConnection connection)
    {
        _connections.Add(connection);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var toRemove = _connections.Where(x => !x.IsHealthy()).ToList();

                if (!toRemove.Any()) continue;

                using var cts = new CancellationTokenSource(_stopTimeout);
                await Task.WhenAll(toRemove.Select(async x =>
                {
                    try
                    {
                        await x.StopAsync(cts.Token).ConfigureAwait(false);
                        _connections.Remove(x);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Failed to stop connection");
                    }
                    finally
                    {
                        x.Dispose();
                    }
                }));
            }
            catch (Exception e) when (stoppingToken.IsCancellationRequested)
            {
                TcpProxyHelper.OnCancelled(e, _logger);
                break;
            }

            try
            {
                await Task.Delay(_gcInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                TcpProxyHelper.OnCancelled(e, _logger);
                break;
            }
        }
    }
}