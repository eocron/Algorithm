using System;
using System.Buffers;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Eocron.ProxyHost;

public class TcpProxyBuilder : IProxyBuilder
{
    public TcpProxySettings Settings { get; set; } = new TcpProxySettings();
    public Action<TcpClient> ConfigureDownStreamDelegate { get; set; } = (x) => { x.NoDelay = true;};
    public Action<TcpClient> ConfigureUpStreamDelegate { get; set; } = (x) => { x.NoDelay = true;};
    public ArrayPool<byte> Pool { get; set; } = ArrayPool<byte>.Shared;

    public Action<ILoggingBuilder> ConfigureLoggingBuilderDelegate { get; set; } = (x) =>
    {
        x.ClearProviders();
        x.AddProvider(NullLoggerProvider.Instance);
    };

    public IProxy Build()
    {
        var services = new ServiceCollection();
        services
            .AddLogging(ConfigureLoggingBuilderDelegate)
            .AddSingleton<TcpUpStreamConnectionProducer>(x =>
                new TcpUpStreamConnectionProducer(
                    TcpProxyHelper.CreateTcpListener(
                        (ushort)Settings.UpStreamPort,
                        Settings.UpStreamHost),
                    Settings,
                    Pool,
                    ConfigureUpStreamDelegate,
                    ConfigureDownStreamDelegate,
                    x.GetRequiredService<ILoggerFactory>(),
                    x.GetRequiredService<ILogger<TcpUpStreamConnectionProducer>>()))
            .AddSingleton<IHostedService>(x => x.GetRequiredService<TcpUpStreamConnectionProducer>())
            .AddSingleton<ConnectionWatcher>(x => new ConnectionWatcher(x.GetRequiredService<ILogger<ConnectionWatcher>>(), Settings.StopTimeout, Settings.WatcherCheckInterval))
            .AddSingleton<IConnectionWatcher>(x => x.GetRequiredService<ConnectionWatcher>())
            .AddSingleton<IHostedService>(x => x.GetRequiredService<ConnectionWatcher>())
            .AddSingleton<IHostedService>(x =>
                new ProxyHandler(
                    x.GetRequiredService<TcpUpStreamConnectionProducer>(),
                    x.GetRequiredService<IConnectionWatcher>(),
                    x.GetRequiredService<ILogger<ProxyHandler>>()));

        var provider = services.BuildServiceProvider();
        return new ThreadSafeProxy(
            new ProxyStartup(
                provider,
                provider.GetRequiredService<ILogger<ProxyStartup>>(),
                Settings.StopTimeout));
    }


}