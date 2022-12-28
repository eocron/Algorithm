using System;
using System.Collections.Generic;
using System.ComponentModel;
using App.Metrics;
using Eocron.Sharding.Configuration;
using Eocron.Sharding.Jobs;
using Eocron.Sharding.Monitoring;
using Eocron.Sharding.Processing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Eocron.Sharding
{
    public sealed class ShardFactory<TInput, TOutput, TError> : IShardFactory<TInput, TOutput, TError>
    {
        public ShardFactory(
            ILoggerFactory loggerFactory,
            IMetrics metrics,
            IStreamReaderDeserializer<TOutput> outputDeserializer,
            IStreamReaderDeserializer<TError> errorDeserializer,
            IStreamWriterSerializer<TInput> inputSerializer,
            IChildProcessWatcher killer,
            ProcessShardOptions options,
            TimeSpan jobErrorRestartInterval,
            TimeSpan jobSuccessRestartInterval,
            TimeSpan metricCollectionInterval)
        {
            _loggerFactory = loggerFactory;
            _metrics = metrics;
            _outputDeserializer = outputDeserializer;
            _errorDeserializer = errorDeserializer;
            _inputSerializer = inputSerializer;
            _killer = killer;
            _options = options;
            _jobErrorRestartInterval = jobErrorRestartInterval;
            _jobSuccessRestartInterval = jobSuccessRestartInterval;
            _metricCollectionInterval = metricCollectionInterval;
        }

        public IShard<TInput, TOutput, TError> CreateNewShard(string id)
        {
            var container = new ServiceCollection();

            var tags = new Dictionary<string, string>
            {
                { "shard_id", id },
                { "input_type", typeof(TInput).Name },
                { "output_type", typeof(TOutput).Name },
                { "error_type", typeof(TError).Name }
            };
            container.AddSingleton<ILogger>(_ => _loggerFactory.CreateLogger<IShard<TInput, TOutput, TError>>());
            container.AddSingleton(_ => _metrics);
            container.AddSingleton<IProcessJob<TInput, TOutput, TError>>(x =>
                    new ProcessJob<TInput, TOutput, TError>(
                        _options,
                        _outputDeserializer,
                        _errorDeserializer,
                        _inputSerializer,
                        x.GetRequiredService<ILogger>(),
                        id: id,
                        watcher: _killer,
                        stateProvider: x.GetService<IProcessStateProvider>()))
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
                    _jobErrorRestartInterval,
                    _jobSuccessRestartInterval));

            AddAppMetrics(
                container, 
                tags, 
                _metricCollectionInterval, 
                _jobErrorRestartInterval,
                _jobSuccessRestartInterval);

            return new ShardContainerAdapter<TInput, TOutput, TError>(container.BuildServiceProvider());
        }

        private static IServiceCollection AddAppMetrics(
            IServiceCollection container, 
            IReadOnlyDictionary<string, string> tags,
            TimeSpan metricCollectionInterval,
            TimeSpan errorRestartInterval,
            TimeSpan successRestartInterval)
        {

            return container
                .Replace<IShardInputManager<TInput>>((x, prev) =>
                    new MonitoredShardInputManager<TInput>(prev, x.GetRequiredService<IMetrics>(), tags))
                .Replace<IShardOutputProvider<TOutput, TError>>((x, prev) =>
                    new MonitoredShardOutputProvider<TOutput, TError>(prev, x.GetRequiredService<IMetrics>(), tags))
                .Replace<IJob>((x, prev) =>
                    new CompoundJob(
                        prev,
                        new RestartUntilCancelledJob(
                            new ShardMonitoringJob<TInput>(
                                x.GetRequiredService<IShardInputManager<TInput>>(),
                                x.GetRequiredService<IProcessDiagnosticInfoProvider>(),
                                x.GetRequiredService<IMetrics>(),
                                metricCollectionInterval,
                                tags),
                            x.GetRequiredService<ILogger>(),
                            errorRestartInterval,
                            successRestartInterval)));
        }

        private readonly IChildProcessWatcher _killer;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMetrics _metrics;
        private readonly IStreamReaderDeserializer<TError> _errorDeserializer;
        private readonly IStreamReaderDeserializer<TOutput> _outputDeserializer;
        private readonly IStreamWriterSerializer<TInput> _inputSerializer;
        private readonly ProcessShardOptions _options;
        private readonly TimeSpan _jobErrorRestartInterval;
        private readonly TimeSpan _jobSuccessRestartInterval;
        private readonly TimeSpan _metricCollectionInterval;
    }
}