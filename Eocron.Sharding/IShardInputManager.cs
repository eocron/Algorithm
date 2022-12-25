using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Sharding
{
    public interface IShardInputManager<in TInput>
    {
        /// <summary>
        ///     Checks if shard is ready to process data
        /// </summary>
        /// <returns></returns>
        bool IsReady();

        /// <summary>
        ///     Publish messages to the shard
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task PublishAsync(IEnumerable<TInput> messages, CancellationToken ct);
    }
}