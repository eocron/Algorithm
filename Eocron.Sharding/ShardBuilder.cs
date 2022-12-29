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

        public ShardBuilder<TInput, TOutput, TError> WithTransient<TInterface, TImplementation>(TImplementation implementation)
            where TImplementation : TInterface
            where TInterface : class
        {
            Add((s, id)=> s.AddTransient<TInterface>(sp=> implementation));
            return this;
        }

        public ShardBuilder<TInput, TOutput, TError> WithTransient<TInterface>(TInterface implementation)
            where TInterface : class
        {
            Add((s, id) => s.AddTransient<TInterface>(sp => implementation));
            return this;
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