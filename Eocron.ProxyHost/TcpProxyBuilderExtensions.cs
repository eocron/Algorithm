using System;
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
    }
}