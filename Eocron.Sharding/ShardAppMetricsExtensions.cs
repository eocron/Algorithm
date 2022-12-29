using System;
using System.Collections.Generic;
using App.Metrics;
using Eocron.Sharding.Jobs;
using Eocron.Sharding.Monitoring;
using Eocron.Sharding.Processing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Eocron.Sharding
{
    public static class ShardAppMetricsExtensions
    {
        public static ShardBuilder<TInput, TOutput, TError> WithAppMetrics<TInput, TOutput, TError>(
            this ShardBuilder<TInput, TOutput, TError> builder, 
            AppMetricsShardOptions options)
        {
            builder.Add((s, shardId)=> AddAppMetrics<TInput, TOutput, TError>(s, options));
            return builder;
        }
        private static IServiceCollection AddAppMetrics<TInput, TOutput, TError>(
            IServiceCollection container,
            AppMetricsShardOptions options)
        {
            return container
                .Replace<IShardInputManager<TInput>>((x, prev) =>
                    new MonitoredShardInputManager<TInput>(prev, x.GetRequiredService<IMetrics>(),
                        Merge(
                            options.Tags,
                            new[]
                            {
                                new KeyValuePair<string, string>("shard_id", x.GetRequiredService<IShard>().Id)
                            })))
                .Replace<IShardOutputProvider<TOutput, TError>>((x, prev) =>
                    new MonitoredShardOutputProvider<TOutput, TError>(prev, x.GetRequiredService<IMetrics>(),
                        Merge(
                            options.Tags,
                            new[]
                            {
                                new KeyValuePair<string, string>("shard_id", x.GetRequiredService<IShard>().Id)
                            })))
                .Replace<IJob>((x, prev) =>
                    new CompoundJob(
                        prev,
                        new RestartUntilCancelledJob(
                            new ShardMonitoringJob<TInput>(
                                x.GetRequiredService<ILogger>(),
                                x.GetRequiredService<IShardInputManager<TInput>>(),
                                x.GetRequiredService<IProcessDiagnosticInfoProvider>(),
                                x.GetRequiredService<IMetrics>(),
                                options.CheckInterval,
                                options.CheckTimeout,
                                Merge(
                                    options.Tags,
                                    new[]
                                    {
                                        new KeyValuePair<string, string>("shard_id", x.GetRequiredService<IShard>().Id)
                                    })),
                            x.GetRequiredService<ILogger>(),
                            options.ErrorRestartInterval,
                            TimeSpan.Zero)));
        }

        private static IReadOnlyDictionary<string, string> Merge(IEnumerable<KeyValuePair<string, string>> a,
            IEnumerable<KeyValuePair<string, string>> b)
        {
            var result = new Dictionary<string, string>();
            if (a != null)
            {
                foreach (var keyValuePair in a)
                {
                    result.Add(keyValuePair.Key, keyValuePair.Value);
                }
            }

            if (b != null)
            {
                foreach (var keyValuePair in b)
                {
                    result.Add(keyValuePair.Key, keyValuePair.Value);
                }
            }

            return result;
        }
    }
}