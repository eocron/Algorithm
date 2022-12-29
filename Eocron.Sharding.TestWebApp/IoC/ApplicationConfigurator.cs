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
            services.AddSingleton(x =>
                new ShardBuilder<string, string, string>()
                    .WithLogging(x.GetRequiredService<ILoggerFactory>())
                    .WithProcessJobDependencies(
                        x.GetRequiredService<IStreamWriterSerializer<string>>(),
                        x.GetRequiredService<IStreamReaderDeserializer<string>>(),
                        x.GetRequiredService<IStreamReaderDeserializer<string>>(),
                        x.GetRequiredService<ILoggerFactory>(),
                        x.GetService<IProcessStateProvider>(),
                        x.GetRequiredService<IChildProcessWatcher>())
                    .WithProcessJob(
                        new ProcessShardOptions
                                {
                                    StartInfo = new ProcessStartInfo("Tools/Eocron.Sharding.TestApp.exe", "stream")
                                        .ConfigureAsService()
                                },
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(5))
                    .WithAppMetrics(
                        x.GetRequiredService<IMetrics>(),
                        null,
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(5))
                    .CreateFactory());
            services.AddSingleton(x =>
                new ConstantShardPool<string, string, string>(
                    x.GetRequiredService<IShardFactory<string, string, string>>(),
                    3));
            services.AddSingleton<IShardPool<string, string, string>>(x => x.GetRequiredService<ConstantShardPool<string, string, string>>());
            services.AddSingleton<IHostedService>(x => x.GetRequiredService<ConstantShardPool<string, string, string>>());
            services.AddSingleton<IShardProvider<string, string, string>>(x => x.GetRequiredService<IShardPool<string, string, string>>());
        }
    }
}
