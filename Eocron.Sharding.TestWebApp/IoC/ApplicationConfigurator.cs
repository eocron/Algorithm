using App.Metrics;
using Eocron.Sharding.Configuration;
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

            services.AddSingleton<IStreamReaderDeserializer<string>, NewLineDeserializer>();
            services.AddSingleton<IStreamWriterSerializer<string>, NewLineSerializer>();
            services.AddSingleton<IShardFactory<string, string, string>>(x =>
                new ShardFactory<string, string, string>(
                    x.GetRequiredService<ILoggerFactory>(),
                    x.GetRequiredService<IMetrics>(),
                    x.GetRequiredService<IStreamReaderDeserializer<string>>(),
                    x.GetRequiredService<IStreamReaderDeserializer<string>>(),
                    x.GetRequiredService<IStreamWriterSerializer<string>>(),
                    "Tools/Eocron.Sharding.TestApp.exe",
                    "stream"));
            services.AddSingleton<IShardPool<string, string, string>>(x =>
                new ConstantShardPool<string, string, string>(
                    x.GetRequiredService<IShardFactory<string, string, string>>(),
                    0));
            services.AddSingleton<IHostedService>(x => x.GetRequiredService<IShardPool<string, string, string>>());
            services.AddSingleton<IShardProvider<string, string, string>>(x => x.GetRequiredService<IShardPool<string, string, string>>());
        }
    }
}
