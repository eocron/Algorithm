using Eocron.Sharding.Jobs;
using Eocron.Sharding.Processing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace Eocron.Sharding
{
    public static class ShardCoreExtensions
    {
        public static IServiceCollection AddShardProcessWatcherHostedService(this IServiceCollection services)
        {
            services.AddSingleton<ChildProcessWatcher>(x => new ChildProcessWatcher(x.GetRequiredService<ILogger<ChildProcessWatcher>>()));
            services.AddSingleton<IChildProcessWatcher>(x => x.GetRequiredService<ChildProcessWatcher>());
            services.AddSingleton<IHostedService>(x => new JobHostedService(x.GetRequiredService<ChildProcessWatcher>()));
            return services;
        }

        public static ShardBuilder<TInput, TOutput, TError> WithLogging<TInput, TOutput, TError>(
            this ShardBuilder<TInput, TOutput, TError> builder,
            ILoggerFactory loggerFactory)
        {
            builder.Add((s, shardId) =>
                s.AddSingleton<ILogger>(_ => loggerFactory.CreateLogger<IShard<TInput, TOutput, TError>>()));
            return builder;
        }

        public static ShardBuilder<TInput, TOutput, TError> WithProcessJob<TInput, TOutput, TError>(
            this ShardBuilder<TInput, TOutput, TError> builder,
            Func<IServiceProvider, string, IProcessJob<TInput, TOutput, TError>> processJobProvider,
            TimeSpan jobErrorRestartInterval,
            TimeSpan jobSuccessRestartInterval)
        {
            builder.Add((s, shardId) => AddCoreDependencies(s, shardId, processJobProvider, jobErrorRestartInterval, jobSuccessRestartInterval));
            return builder;
        }

        private static IServiceCollection AddCoreDependencies<TInput, TOutput, TError>(
            IServiceCollection container,
            string shardId,
            Func<IServiceProvider, string, IProcessJob<TInput, TOutput, TError>> processJobProvider,
            TimeSpan jobErrorRestartInterval,
            TimeSpan jobSuccessRestartInterval)
        {
            container.AddSingleton<IProcessJob<TInput, TOutput, TError>>(x => processJobProvider(x, shardId))
                .AddSingleton<IShard>(x =>
                    x.GetRequiredService<IProcessJob<TInput, TOutput, TError>>())
                .AddSingleton<IProcessDiagnosticInfoProvider>(x =>
                    x.GetRequiredService<IProcessJob<TInput, TOutput, TError>>())
                .AddSingleton<IShardInputManager<TInput>>(x =>
                    x.GetRequiredService<IProcessJob<TInput, TOutput, TError>>())
                .AddSingleton<IShardOutputProvider<TOutput, TError>>(x =>
                    x.GetRequiredService<IProcessJob<TInput, TOutput, TError>>())
                .AddSingleton<IJob>(x =>
                    x.GetRequiredService<IProcessJob<TInput, TOutput, TError>>())
                .Replace<IJob, ShardLifetimeJob>((x, prev) => new ShardLifetimeJob(prev, x.GetRequiredService<ILogger>(), true))
                .AddSingleton<IShardLifetimeManager>(x => x.GetRequiredService<ShardLifetimeJob>())
                .Replace<IJob>((x, prev) => new RestartUntilCancelledJob(
                    prev,
                    x.GetRequiredService<ILogger>(),
                    jobErrorRestartInterval,
                    jobErrorRestartInterval));
            return container;
        }
    }
}