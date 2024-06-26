using System;
using System.Buffers;
using System.Net.Sockets;

namespace Eocron.ProxyHost
{
    public class TcpProxyBuilder : IProxyBuilder
    {
        public TcpProxySettings Settings { get; set; } = new TcpProxySettings();
        public Action<TcpClient> ConfigureDownStream { get; set; }
        public Action<TcpClient> ConfigureUpStream { get; set; }
        public ArrayPool<byte> Pool { get; set; } = ArrayPool<byte>.Shared;
        public IProxy Build()
        {
            return new ThreadSafeProxy(new DefaultProxy(
                new TcpUpStreamConnectionProducer(
                    TcpProxyHelper.CreateTcpListener(
                        (ushort)Settings.LocalPort,
                        Settings.LocalIpAddress),
                    Settings,
                    Pool,
                    ConfigureUpStream,
                    ConfigureDownStream),
                new ConnectionWatcher(Settings.WatcherStopTimeout)));
        }


    }
}