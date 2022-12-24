using System.Diagnostics;
using App.Metrics;
using Eocron.Sharding.Configuration;
using Eocron.Sharding.Monitoring;
using Eocron.Sharding.Processing;

namespace Eocron.Sharding.TestWebApp.Shards
{
    public sealed class ShardFactory<TInput, TOutput, TError> : IShardFactory<TInput, TOutput, TError>
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMetrics _metrics;
        private readonly IStreamReaderDeserializer<TOutput> _outputDeserializer;
        private readonly IStreamReaderDeserializer<TError> _errorDeserializer;
        private readonly IStreamWriterSerializer<TInput> _inputSerializer;
        private readonly IChildProcessKiller _killer;
        private readonly string _filePath;
        private readonly string _args;

        public ShardFactory(
            ILoggerFactory loggerFactory, 
            IMetrics metrics, 
            IStreamReaderDeserializer<TOutput> outputDeserializer, 
            IStreamReaderDeserializer<TError> errorDeserializer, 
            IStreamWriterSerializer<TInput> inputSerializer,
            IChildProcessKiller killer,
            string filePath,
            string args)
        {
            _loggerFactory = loggerFactory;
            _metrics = metrics;
            _outputDeserializer = outputDeserializer;
            _errorDeserializer = errorDeserializer;
            _inputSerializer = inputSerializer;
            _killer = killer;
            _filePath = filePath;
            _args = args;
        }

        public IShard<TInput, TOutput, TError> CreateNewShard(string id)
        {
            var logger = _loggerFactory.CreateLogger<IShard<TInput, TOutput, TError>>();
            return new RestartInfinitelyShard<TInput, TOutput, TError>(
                new AppMetricsProcessShard<TInput, TOutput, TError>(
                    new ProcessShard<TInput, TOutput, TError>(
                        new ProcessShardOptions
                        {
                            StartInfo = new ProcessStartInfo(_filePath, _args)
                                .ConfigureAsService()
                        },
                        _outputDeserializer,
                        _errorDeserializer,
                        _inputSerializer,
                        logger,
                        id: id,
                        childProcessKiller: _killer),
                    _metrics,
                    TimeSpan.FromSeconds(5)),
                logger,
                TimeSpan.FromSeconds(5),
                TimeSpan.Zero);
        }
    }
}
