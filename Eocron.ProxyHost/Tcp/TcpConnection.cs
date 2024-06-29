using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Eocron.ProxyHost.Tcp;

public sealed class TcpConnection : BackgroundService, IProxyConnection
{
    private readonly TcpClient _downStreamClient;
    private readonly TcpClient _upStreamClient;
    private readonly IPEndPoint _downStreamEndpoint;
    private readonly ArrayPool<byte> _pool;
    private readonly TcpProxySettings _settings;
    private readonly ILogger _logger;
    private long _totalBytesForwarded;
    private long _totalBytesResponded;
    private long _lastActivity = Environment.TickCount64;
    private bool _disposed;
    private bool _isStopped;

    public TcpConnection(TcpClient upStreamClient, TcpClient downStreamClient, IPEndPoint downStreamEndpoint, ArrayPool<byte> pool, TcpProxySettings settings, ILogger logger)
    {
        _downStreamClient = downStreamClient;
        _upStreamClient = upStreamClient;
        _downStreamEndpoint = downStreamEndpoint;
        _pool = pool;
        _settings = settings;
        _logger = logger;
    }

    private async Task CopyToAsync(Stream source, Stream destination, int bufferSize, Direction direction, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        var buffer = _pool.Rent(bufferSize);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await source.ReadAsync(new Memory<byte>(buffer), cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0) break;
                Interlocked.Exchange(ref _lastActivity, Environment.TickCount64);
                await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancellationToken).ConfigureAwait(false);

                switch (direction)
                {
                    case Direction.DownStream:
                        Interlocked.Add(ref _totalBytesForwarded, bytesRead);
                        break;
                    case Direction.UpStream:
                        Interlocked.Add(ref _totalBytesResponded, bytesRead);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                }
            }
        }
        finally
        {
            _pool.Return(buffer);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        await _downStreamClient.ConnectAsync(_downStreamEndpoint.Address, _downStreamEndpoint.Port, cts.Token).ConfigureAwait(false);
        await using var serverStream = _downStreamClient.GetStream();
        await using var clientStream = _upStreamClient.GetStream();
        await using (cts.Token.Register(() =>
                     {
                         SafeClose(serverStream);
                         SafeClose(clientStream);
                     }, true))
        {
            await Task.WhenAny(
                CopyToAsync(clientStream, serverStream, _settings.DownStreamBufferSize, Direction.DownStream, cts.Token),
                CopyToAsync(serverStream, clientStream, _settings.UpStreamBufferSize, Direction.UpStream, cts.Token)
            ).ConfigureAwait(false);
            cts.Cancel();
            _isStopped = true;
        }
    }

    public bool IsHealthy()
    {
        return !_isStopped && !_disposed && (_lastActivity + _settings.ConnectionTimeout.Ticks > Environment.TickCount64);
    }

    public override void Dispose()
    {
        if(_disposed)
            return;
        
        SafeDispose(_upStreamClient);
        SafeDispose(_downStreamClient);
        base.Dispose();
        _disposed = true;
    }

    private static void SafeDispose(TcpClient client)
    {
        try
        {
            client?.Close();
        }
        catch
        {
            // ignored
        }

        try
        {
            client?.Dispose();
        }
        catch
        {
            // ignored
        }
    }

    private static void SafeClose(Stream stream)
    {
        try
        {
            stream?.Close();
        }
        catch
        {
            // ignored
        }
    }

    private enum Direction
    {
        DownStream,
        UpStream,
    }
}