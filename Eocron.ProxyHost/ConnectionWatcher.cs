﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Eocron.ProxyHost;

public sealed class ConnectionWatcher : BackgroundService, IConnectionWatcher
{
    private readonly ILogger _logger;
    private readonly TimeSpan _stopTimeout;
    private readonly TimeSpan _gcInterval;
    private readonly ConcurrentDictionary<IProxyConnection, object> _connections = new ConcurrentDictionary<IProxyConnection, object>();
        
    public ConnectionWatcher(ILogger logger, TimeSpan stopTimeout, TimeSpan gcInterval)
    {
        _logger = logger;
        _stopTimeout = stopTimeout;
        _gcInterval = gcInterval;
    }

    public void Watch(IProxyConnection connection)
    {
        _connections.TryAdd(connection, null);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var toRemove = _connections.Where(x => !x.Key.IsHealthy()).ToList();

                if (!toRemove.Any()) continue;

                using var cts = new CancellationTokenSource(_stopTimeout);
                await Task.WhenAll(toRemove.Select(async x =>
                {
                    try
                    {
                        await x.Key.StopAsync(cts.Token).ConfigureAwait(false);
                        _connections.TryRemove(x);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Failed to stop connection");
                    }
                }));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogTrace("Cancelled");
                break;
            }

            try
            {
                await Task.Delay(_gcInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogTrace("Cancelled");
                break;
            }
        }
    }
}