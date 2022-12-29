using System.Diagnostics;
using App.Metrics;
using Eocron.Sharding.Configuration;
using Eocron.Sharding.Processing;
using Eocron.Sharding.TestWebApp.Shards;

namespace Eocron.Sharding.TestWebApp.IoC
{
    public static class ApplicationConfigurator
    {
        public static void Configure(IServiceCollection services)
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddMetrics();
            services.AddMetricsEndpoints();
            services.AddShardProcessWatcherHostedService();
            services.AddSingleton<IStreamReaderDeserializer<string>, NewLineDeserializer>();
            services.AddSingleton<IStreamWriterSerializer<string>, NewLineSerializer>();
            services.AddSingleton<IShardFactory<string, string, string>>(x =>
                new ShardBuilder<string, string, string>()
                    .WithLogging(x.GetRequiredService<ILoggerFactory>())
                    .WithProcessJob((s, id) =>
                            new ProcessJob<string, string, string>(
                                new ProcessShardOptions
                                {
                                    StartInfo = new ProcessStartInfo("Tools/Eocron.Sharding.TestApp.exe", "stream")
                                        .ConfigureAsService()
                                },
                                x.GetRequiredService<IStreamReaderDeserializer<string>>(),
                                x.GetRequiredService<IStreamReaderDeserializer<string>>(),
                                x.GetRequiredService<IStreamWriterSerializer<string>>(),
                                s.GetRequiredService<ILogger>(),
                                id: id,
                                watcher: x.GetRequiredService<IChildProcessWatcher>(),
                                stateProvider: x.GetService<IProcessStateProvider>()),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(5))
                    .WithAppMetrics(
                        x.GetRequiredService<IMetrics>(),
                        null,
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(5))
                    .CreateFactory());
            services.AddSingleton<IShardPool<string, string, string>>(x =>
                new ConstantShardPool<string, string, string>(
                    x.GetRequiredService<IShardFactory<string, string, string>>(),
                    3));
            services.AddSingleton<IHostedService>(x => x.GetRequiredService<IShardPool<string, string, string>>());
            services.AddSingleton<IShardProvider<string, string, string>>(x => x.GetRequiredService<IShardPool<string, string, string>>());
        }
    }
}
