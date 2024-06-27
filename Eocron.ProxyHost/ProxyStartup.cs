using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Eocron.ProxyHost.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Eocron.ProxyHost
{
    public sealed class ProxyStartup : IProxy
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly ConcurrentList<IHostedService> _started = new();
        private readonly TimeSpan _onStartupFailStopTimeout;
        public EndPoint UpStreamEndpoint => _serviceProvider.GetRequiredService<IProxyUpStreamConnectionProducer>().UpStreamEndpoint;
        public ProxyStartup(ServiceProvider serviceProvider, ILogger logger, TimeSpan onStartupFailStopTimeout)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _onStartupFailStopTimeout = onStartupFailStopTimeout;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
            try
            {
                var toStart = _serviceProvider.GetServices<IHostedService>().ToList();
                await Task.WhenAll(toStart.Select(async x =>
                {
                    await x.StartAsync(cancellationToken).ConfigureAwait(false);
                    _started.Add(x);
                }));
                _logger.LogInformation("Proxy started on {endpoint}", UpStreamEndpoint);
            }
            catch (Exception e1)
            {
                _logger.LogError(e1, "Failed to start proxy internal services. Stopping already started.");
                try
                {
                    using var cts = new CancellationTokenSource(_onStartupFailStopTimeout);
                    await StopAsync(cts.Token).ConfigureAwait(false);
                }
                catch (Exception e2)
                {
                    throw new AggregateException(e1, e2);
                }

                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
            if (_started.Any())
            {
                try
                {
                    await Task.WhenAll(_started.Select(async x =>
                    {
                        await x.StopAsync(cancellationToken).ConfigureAwait(false);
                        _started.Remove(x);
                    }));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to stop proxy services");
                    throw;
                }
            }
        }
    }
}