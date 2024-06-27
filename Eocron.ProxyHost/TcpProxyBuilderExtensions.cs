using System;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Eocron.ProxyHost
{
    public static class TcpProxyBuilderExtensions
    {
        public static TcpProxyBuilder ConfigureLogging(this TcpProxyBuilder builder, Action<ILoggingBuilder> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));
        
            var tmp = builder.ConfigureLoggingBuilderDelegate;
            builder.ConfigureLoggingBuilderDelegate = x =>
            {
                tmp?.Invoke(x);
                configure.Invoke(x);
            };
            return builder;
        }

        public static TcpProxyBuilder ConfigureUpStreamTcpClient(this TcpProxyBuilder builder,
            Action<TcpClient> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));
        
            var tmp = builder.ConfigureUpStreamDelegate;
            builder.ConfigureUpStreamDelegate = x =>
            {
                tmp?.Invoke(x);
                configure.Invoke(x);
            };
            return builder;
        }
        
        public static TcpProxyBuilder ConfigureDownStreamTcpClient(this TcpProxyBuilder builder,
            Action<TcpClient> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));
        
            var tmp = builder.ConfigureDownStreamDelegate;
            builder.ConfigureDownStreamDelegate = x =>
            {
                tmp?.Invoke(x);
                configure.Invoke(x);
            };
            return builder;
        }
    }
}