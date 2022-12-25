using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DryIoc;
using Eocron.Sharding.Jobs;

namespace Eocron.Sharding
{
    public class ShardContainerAdapter<TInput, TOutput, TError> : IShard<TInput, TOutput, TError>
    {
        public ShardContainerAdapter(IContainer container)
        {
            _container = container;
        }

        public async Task CancelAsync(CancellationToken ct)
        {
            await _container.Resolve<ICancellationManager>().CancelAsync(ct).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _container.Dispose();
        }

        public bool IsReady()
        {
            return _container.Resolve<IShardInputManager<TInput>>().IsReady();
        }

        public async Task PublishAsync(IEnumerable<TInput> messages, CancellationToken ct)
        {
            await _container.Resolve<IShardInputManager<TInput>>().PublishAsync(messages, ct).ConfigureAwait(false);
        }

        public async Task RunAsync(CancellationToken ct)
        {
            await _container.Resolve<IJob>().RunAsync(ct).ConfigureAwait(false);
        }

        public bool TryCancel()
        {
            return _container.Resolve<ICancellationManager>().TryCancel();
        }

        public ChannelReader<ShardMessage<TError>> Errors =>
            _container.Resolve<IShardOutputProvider<TOutput, TError>>().Errors;

        public ChannelReader<ShardMessage<TOutput>> Outputs =>
            _container.Resolve<IShardOutputProvider<TOutput, TError>>().Outputs;

        public string Id => _container.Resolve<IShard>().Id;
        private readonly IContainer _container;
    }
}