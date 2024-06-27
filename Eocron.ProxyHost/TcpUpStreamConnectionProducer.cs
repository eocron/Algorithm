using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Eocron.ProxyHost.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Eocron.ProxyHost;

public sealed class TcpUpStreamConnectionProducer : IProxyUpStreamConnectionProducer, IHostedService
{
    private readonly TcpListener _listener;
    private readonly TcpProxySettings _settings;
    private readonly ArrayPool<byte> _pool;
    private readonly Action<TcpClient> _configureUpStream;
    private readonly Action<TcpClient> _configureDownStream;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    public EndPoint UpStreamEndpoint => _listener.LocalEndpoint;
    public TcpUpStreamConnectionProducer(
        TcpListener listener,
        TcpProxySettings settings,
        ArrayPool<byte> pool,
        Action<TcpClient> configureUpStream,
        Action<TcpClient> configureDownStream,
        ILoggerFactory loggerFactory,
        ILogger logger)
    {
        _listener = listener;
        _settings = settings;
        _pool = pool;
        _configureUpStream = configureUpStream;
        _configureDownStream = configureDownStream;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public async IAsyncEnumerable<IProxyConnection> GetPendingConnections([EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            IProxyConnection toReturn = null;
            try
            {
                var upStreamClient = await _listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                try
                {
                    var endpoint = await DefaultResolve(_settings.DownStreamHost, _settings.DownStreamPort, ct)
                        .ConfigureAwait(false);
                    _configureUpStream.Invoke(upStreamClient);
                    var downStreamClient = new TcpClient();
                    _configureDownStream.Invoke(downStreamClient);
                    toReturn = new TcpConnection(upStreamClient, downStreamClient, endpoint, _pool, _settings,
                        _loggerFactory.CreateLogger<TcpConnection>());
                }
                catch (Exception)
                {
                    upStreamClient.Close();
                    throw;
                }
            }
            catch (Exception e) when (ct.IsCancellationRequested)
            {
                TcpProxyHelper.OnCancelled(e, _logger);
                break;
            }
            catch (ObjectDisposedException e)
            {
                TcpProxyHelper.OnCancelled(e, _logger);
                break;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to accept upstream connection");
            }

            if (toReturn != null)
            {
                yield return toReturn;
            }
        }
    }

    private static async Task<IPEndPoint> DefaultResolve(string downStreamHost, int remoteServerPort, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(downStreamHost))
        {
            throw new ArgumentNullException(nameof(downStreamHost), "Down stream host is empty");
        }

        if (remoteServerPort <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(remoteServerPort),
                "Down stream port is invalid: " + remoteServerPort);
        }
        var ips = await Dns.GetHostAddressesAsync(downStreamHost, ct).ConfigureAwait(false);
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