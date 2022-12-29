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
            IMetrics metrics,
            IReadOnlyDictionary<string, string> tags,
            TimeSpan metricCollectionInterval,
            TimeSpan errorRestartInterval)
        {
            builder.Add((s, shardId)=> AddAppMetrics<TInput, TOutput, TError>(s, metrics, tags, metricCollectionInterval, errorRestartInterval));
            return builder;
        }
        private static IServiceCollection AddAppMetrics<TInput, TOutput, TError>(
            IServiceCollection container,
            IMetrics metrics,
            IReadOnlyDictionary<string, string> tags,
            TimeSpan metricCollectionInterval,
            TimeSpan errorRestartInterval)
        {
            return container
                .Replace<IShardInputManager<TInput>>((x, prev) =>
                    new MonitoredShardInputManager<TInput>(prev, metrics,
                        Merge(
                            tags,
                            new[]
                            {
                                new KeyValuePair<string, string>("shard_id", x.GetRequiredService<IShard>().Id)
                            })))
                .Replace<IShardOutputProvider<TOutput, TError>>((x, prev) =>
                    new MonitoredShardOutputProvider<TOutput, TError>(prev, metrics,
                        Merge(
                            tags,
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
                                metrics,
                                metricCollectionInterval,
                                metricCollectionInterval,
                                Merge(
                                    tags,
                                    new[]
                                    {
                                        new KeyValuePair<string, string>("shard_id", x.GetRequiredService<IShard>().Id)
                                    })),
                            x.GetRequiredService<ILogger>(),
                            errorRestartInterval,
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