using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Sharding.Processing
{
    public interface IShardInputManager<in TInput>
    {
        /// <summary>
        ///     Checks if shard is ready to process data
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<bool> IsReadyAsync(CancellationToken ct);

        /// <summary>
        ///     Publish messages to the shard
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task PublishAsync(IEnumerable<TInput> messages, CancellationToken ct);
    }
}