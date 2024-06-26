using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Eocron.ProxyHost;

public sealed class ConnectionWatcher : BackgroundService, IConnectionWatcher
{
    private readonly TimeSpan _stopTimeout;
    private readonly ConcurrentBag<IProxyConnection> _connections = new ConcurrentBag<IProxyConnection>();
        
    public ConnectionWatcher(TimeSpan stopTimeout)
    {
        _stopTimeout = stopTimeout;
    }

    public void Watch(IProxyConnection connection)
    {
        _connections.Add(connection);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var toRemove = _connections.Where(x => !x.IsHealthy()).ToList();

            if (!toRemove.Any()) continue;
                
            using var cts = new CancellationTokenSource(_stopTimeout);
            await Task.WhenAll(toRemove.Select(x => x.StopAsync(cts.Token)));
        }
    }
}