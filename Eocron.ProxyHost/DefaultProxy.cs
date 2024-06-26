using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Eocron.ProxyHost
{
    public sealed class DefaultProxy : BackgroundService, IProxy
    {
        private readonly IProxyUpStreamConnectionProducer _producer;
        private readonly IConnectionWatcher _watcher;

        public DefaultProxy(IProxyUpStreamConnectionProducer producer, IConnectionWatcher watcher)
        {
            _producer = producer;
            _watcher = watcher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await foreach (var pendingConnection in _producer.GetPendingConnections(stoppingToken))
                {
                    await pendingConnection.StartAsync(stoppingToken).ConfigureAwait(false);
                    _watcher.Watch(pendingConnection);
                }
            }
        }
    }
}