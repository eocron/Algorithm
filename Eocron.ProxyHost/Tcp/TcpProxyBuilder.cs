using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using Eocron.ProxyHost.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Eocron.ProxyHost.Tcp;

public class TcpProxyBuilder : IProxyBuilder
{
    public ArrayPool<byte> Pool { get; set; } = ArrayPool<byte>.Shared;
    public TcpProxySettings Settings { get; set; } = new TcpProxySettings();
    internal Action<TcpClient> ConfigureDownStreamDelegate { get; set; } = (x) => { x.NoDelay = true;};
    internal Action<TcpClient> ConfigureUpStreamDelegate { get; set; } = (x) => { x.NoDelay = true;};
    internal Action<ILoggingBuilder> ConfigureLoggingBuilderDelegate { get; set; } = (x) =>
    {
        x.ClearProviders();
        x.AddProvider(NullLoggerProvider.Instance);
    };

    internal DownStreamResolverDelegate EndpointResolver { get; set; } = TcpProxyHelper.DnsResolve;

    public IProxy Build()
    {
        Validate();
        var services = new ServiceCollection();
        services
            .AddLogging(ConfigureLoggingBuilderDelegate)
            .AddSingleton<TcpUpStreamConnectionProducer>(x =>
                new TcpUpStreamConnectionProducer(
                    TcpProxyHelper.CreateTcpListener(Settings.UpStreamHost, (ushort)Settings.UpStreamPort),
                    Settings,
                    Pool,
                    ConfigureUpStreamDelegate,
                    ConfigureDownStreamDelegate,
                    EndpointResolver,
                    x.GetRequiredService<ILoggerFactory>(),
                    x.GetRequiredService<ILogger<TcpUpStreamConnectionProducer>>()))
            .AddSingleton<IHostedService>(x => x.GetRequiredService<TcpUpStreamConnectionProducer>())
            .AddSingleton<IProxyUpStreamConnectionProducer>(x=> x.GetRequiredService<TcpUpStreamConnectionProducer>())
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

    private void Validate()
    {
        if (Settings == null)
            throw new ArgumentNullException(nameof(Settings));
        if (Pool == null)
            throw new ArgumentNullException(nameof(Pool));
        if (string.IsNullOrWhiteSpace(Settings.DownStreamHost))
            throw new ArgumentNullException(nameof(Settings.DownStreamHost));
        if (Settings.DownStreamPort <= 0)
            throw new ArgumentOutOfRangeException(nameof(Settings.DownStreamPort));
    }
}