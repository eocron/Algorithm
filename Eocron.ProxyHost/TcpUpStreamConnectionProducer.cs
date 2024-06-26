using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Eocron.ProxyHost
{
    public sealed class TcpUpStreamConnectionProducer : IProxyUpStreamConnectionProducer, IHostedService
    {
        private readonly TcpListener _listener;
        private readonly TcpProxySettings _settings;
        private readonly ArrayPool<byte> _pool;
        private readonly Action<TcpClient> _configureUpStream;
        private readonly Action<TcpClient> _configureDownStream;

        public TcpUpStreamConnectionProducer(
            TcpListener listener,
            TcpProxySettings settings,
            ArrayPool<byte> pool,
            Action<TcpClient> configureUpStream, 
            Action<TcpClient> configureDownStream)
        {
            _listener = listener;
            _settings = settings;
            _pool = pool;
            _configureUpStream = configureUpStream;
            _configureDownStream = configureDownStream;
        }

        public async IAsyncEnumerable<IProxyConnection> GetPendingConnections([EnumeratorCancellation] CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var endpoint = await DefaultResolve(_settings.RemoteServerHostNameOrAddress, _settings.RemoteServerPort, ct).ConfigureAwait(false);
                var upStreamClient = await _listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                _configureUpStream?.Invoke(upStreamClient);
                var downStreamClient = new TcpClient();
                _configureDownStream?.Invoke(downStreamClient);
                yield return new TcpConnection(upStreamClient, downStreamClient, endpoint, _pool, _settings.BufferSize, _settings.ConnectionTimeout);
            }
        }

        private static async Task<IPEndPoint> DefaultResolve(string remoteServerHostNameOrAddress, int remoteServerPort, CancellationToken ct)
        {
            var ips = await Dns.GetHostAddressesAsync(remoteServerHostNameOrAddress, ct).ConfigureAwait(false);
            var endpoint = new IPEndPoint(ips[0], remoteServerPort);
            return endpoint;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _listener.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _listener.Stop();
            return Task.CompletedTask;
        }
    }
}