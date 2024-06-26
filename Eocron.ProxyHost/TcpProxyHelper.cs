﻿using System.Net;
using System.Net.Sockets;

namespace Eocron.ProxyHost;

public static class TcpProxyHelper
{
    public static TcpListener CreateTcpListener(ushort localPort, string? localIp)
    {
        IPAddress localIpAddress = string.IsNullOrEmpty(localIp) ? IPAddress.IPv6Any : IPAddress.Parse(localIp);
        var localServer = new TcpListener(new IPEndPoint(localIpAddress, localPort));
        localServer.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        return localServer;
    }
}