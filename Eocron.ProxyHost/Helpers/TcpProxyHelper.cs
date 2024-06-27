using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Eocron.ProxyHost.Helpers;

public static class TcpProxyHelper
{
    public static TcpListener CreateTcpListener(ushort localPort, string? localIp)
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
}