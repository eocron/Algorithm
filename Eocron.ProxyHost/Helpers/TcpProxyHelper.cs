using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Eocron.ProxyHost.Helpers;

internal static class TcpProxyHelper
{
    public static TcpListener CreateTcpListener(string? localIp, ushort localPort)
    {
        var localIpAddress = string.IsNullOrEmpty(localIp) ? IPAddress.IPv6Any : IPAddress.Parse(localIp);
        var localServer = new TcpListener(new IPEndPoint(localIpAddress, localPort));
        localServer.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        return localServer;
    }

    public static void OnCancelled(Exception exception, ILogger logger)
    {
        logger.LogTrace("Cancelled");
    }
    
    public static async Task<IPEndPoint> DnsResolve(string downStreamHost, int downStreamPort, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(downStreamHost))
        {
            throw new ArgumentNullException(nameof(downStreamHost), "Down stream host is empty");
        }

        if (downStreamPort <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(downStreamPort),
                "Down stream port is invalid: " + downStreamPort);
        }
        var ips = await Dns.GetHostAddressesAsync(downStreamHost, ct).ConfigureAwait(false);
        var endpoint = new IPEndPoint(ips[0], downStreamPort);
        return endpoint;
    }
}