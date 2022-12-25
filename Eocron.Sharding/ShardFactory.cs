using System;
using System.Collections.Generic;
using App.Metrics;
using DryIoc;
using Eocron.Sharding.Configuration;
using Eocron.Sharding.Jobs;
using Eocron.Sharding.Monitoring;
using Eocron.Sharding.Processing;
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
            var container = new Container();

            var tags = new Dictionary<string, string>
            {
                { "shard_id", id },
                { "input_type", typeof(TInput).Name },
                { "output_type", typeof(TOutput).Name },
                { "error_type", typeof(TError).Name }
            };
            var logger = _loggerFactory.CreateLogger<IShard<TInput, TOutput, TError>>();

            container.RegisterDelegate(_ => _options, Reuse.Singleton);
            container.RegisterDelegate<ILogger>(_ => logger, Reuse.Singleton);
            container.RegisterDelegate(_ => _outputDeserializer, Reuse.Singleton, serviceKey: "output");
            container.RegisterDelegate(_ => _errorDeserializer, Reuse.Singleton, serviceKey: "error");
            container.RegisterDelegate(_ => _inputSerializer, Reuse.Singleton);
            container.RegisterDelegate(_ => _killer, Reuse.Singleton);
            container.RegisterDelegate(_ => _metrics, Reuse.Singleton);
            container.RegisterDelegate(x =>
                    new ProcessJob<TInput, TOutput, TError>(
                        x.Resolve<ProcessShardOptions>(),
                        x.Resolve<IStreamReaderDeserializer<TOutput>>("output"),
                        x.Resolve<IStreamReaderDeserializer<TError>>("error"),
                        x.Resolve<IStreamWriterSerializer<TInput>>(),
                        x.Resolve<ILogger>(),
                        id: id,
                        watcher: x.Resolve<IChildProcessWatcher>()),
                Reuse.Singleton);
            container.RegisterDelegate<IShard>(x => x.Resolve<ProcessJob<TInput, TOutput, TError>>());
            container.RegisterDelegate<IProcessDiagnosticInfoProvider>(x =>
                x.Resolve<ProcessJob<TInput, TOutput, TError>>());
            container.RegisterDelegate<IShardInputManager<TInput>>(x =>
                    new MonitoredShardInputManager<TInput>(
                        x.Resolve<ProcessJob<TInput, TOutput, TError>>(),
                        x.Resolve<IMetrics>(),
                        tags),
                Reuse.Singleton);
            container.RegisterDelegate<IShardOutputProvider<TOutput, TError>>(x =>
                    new MonitoredShardOutputProvider<TOutput, TError>(
                        x.Resolve<ProcessJob<TInput, TOutput, TError>>(),
                        x.Resolve<IMetrics>(),
                        tags),
                Reuse.Singleton);
            container.RegisterDelegate<IJob>(x =>
                    new RestartUntilCancelledJob(
                        new ShardMonitoringJob<TInput>(
                            x.Resolve<IShardInputManager<TInput>>(),
                            x.Resolve<IProcessDiagnosticInfoProvider>(),
                            x.Resolve<IMetrics>(),
                            _metricCollectionInterval,
                            tags),
                        x.Resolve<ILogger>(),
                        _jobErrorRestartInterval,
                        _jobSuccessRestartInterval),
                Reuse.Singleton,
                serviceKey: "monitoring");

            container.RegisterDelegate<IJob>(x => x.Resolve<ProcessJob<TInput, TOutput, TError>>(), Reuse.Singleton,
                serviceKey: "process");
            container.RegisterDelegate(x => new CancellableJob(x.Resolve<IJob>("process")), Reuse.Singleton);
            container.RegisterDelegate<ICancellationManager>(x => x.Resolve<CancellableJob>(), Reuse.Singleton);
            container.RegisterDelegate<IJob>(x => x.Resolve<CancellableJob>(), Reuse.Singleton,
                serviceKey: "cancellable");

            container.RegisterDelegate<IJob>(x =>
                    new RestartUntilCancelledJob(
                        x.Resolve<IJob>("cancellable"),
                        x.Resolve<ILogger>(),
                        _jobErrorRestartInterval,
                        _jobSuccessRestartInterval),
                Reuse.Singleton,
                serviceKey: "main");

            container.RegisterDelegate<IJob>(x =>
                new CompoundJob(x.Resolve<IJob>("main"), x.Resolve<IJob>("monitoring")), Reuse.Singleton);
            return new ShardContainerAdapter<TInput, TOutput, TError>(container);
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