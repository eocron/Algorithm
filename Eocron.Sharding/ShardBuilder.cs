using Eocron.Sharding.Configuration;
using Eocron.Sharding.Processing;
using Microsoft.Extensions.DependencyInjection;

namespace Eocron.Sharding
{
    public sealed class ShardBuilder<TInput, TOutput, TError>
    {
        public delegate void ConfiguratorStep<TInput, TOutput, TError>(IServiceCollection shardServices, string shardId);
        public ConfiguratorStep<TInput, TOutput, TError> Configurator { get; set; }
        public IStreamWriterSerializer<TInput> InputSerializer { get; set; }
        public IStreamReaderDeserializer<TOutput> OutputDeserializer { get; set; }
        public IStreamReaderDeserializer<TError> ErrorDeserializer { get; set; }
        public IProcessStateProvider ProcessStateProvider { get; set; }
        public IChildProcessWatcher Watcher { get; set; }

        public ShardBuilder()
        {

        }

        public void Add(ConfiguratorStep<TInput, TOutput, TError> next)
        {
            if(next == null)
                return;

            var prev = Configurator;
            Configurator = (s, id) =>
            {
                prev?.Invoke(s, id);
                next(s, id);
            };
        }

        public IShard<TInput, TOutput, TError> Build(string shardId)
        {
            var services = new ServiceCollection();
            Configurator?.Invoke(services, shardId);
            return new ShardContainerAdapter<TInput, TOutput, TError>(services.BuildServiceProvider());
        }

        public IShardFactory<TInput, TOutput, TError> CreateFactory()
        {
            return new ShardFactory<TInput, TOutput, TError>(this);
        }
    }
}