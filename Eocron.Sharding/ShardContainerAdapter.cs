using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Eocron.Sharding.Jobs;
using Eocron.Sharding.Processing;
using Microsoft.Extensions.DependencyInjection;

namespace Eocron.Sharding
{
    public sealed class ShardContainerAdapter<TInput, TOutput, TError> : IShard<TInput, TOutput, TError>
    {
        public ShardContainerAdapter(ServiceProvider container)
        {
            _container = container;
        }

        public void Dispose()
        {
            _container.Dispose();
        }

        public bool IsReady()
        {
            return _container.GetRequiredService<IShardInputManager<TInput>>().IsReady();
        }

        public async Task PublishAsync(IEnumerable<TInput> messages, CancellationToken ct)
        {
            await _container.GetRequiredService<IShardInputManager<TInput>>().PublishAsync(messages, ct).ConfigureAwait(false);
        }

        public async Task RunAsync(CancellationToken ct)
        {
            await _container.GetRequiredService<IJob>().RunAsync(ct).ConfigureAwait(false);
        }

        public ChannelReader<ShardMessage<TError>> Errors =>
            _container.GetRequiredService<IShardOutputProvider<TOutput, TError>>().Errors;

        public ChannelReader<ShardMessage<TOutput>> Outputs =>
            _container.GetRequiredService<IShardOutputProvider<TOutput, TError>>().Outputs;

        public string Id => _container.GetRequiredService<IShard>().Id;
        private readonly ServiceProvider _container;
        public bool IsStopped()
        {
            return _container.GetRequiredService<IShardLifetimeManager>().IsStopped();
        }

        public async Task StopAsync(CancellationToken ct)
        {
            await _container.GetRequiredService<IShardLifetimeManager>().StopAsync(ct).ConfigureAwait(false);
        }

        public bool TryStop()
        {
            return _container.GetRequiredService<IShardLifetimeManager>().TryStop();
        }

        public async Task StartAsync(CancellationToken ct)
        {
            await _container.GetRequiredService<IShardLifetimeManager>().StartAsync(ct).ConfigureAwait(false);
        }

        public async Task RestartAsync(CancellationToken ct)
        {
            await _container.GetRequiredService<IShardLifetimeManager>().RestartAsync(ct).ConfigureAwait(false);
        }
    }
}