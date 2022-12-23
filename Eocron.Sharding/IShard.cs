using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Sharding
{
    public interface IShard<in TInput, out TOutput, out TError>
    {
        /// <summary>
        /// Return output stream of the shard
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        IAsyncEnumerable<TOutput> GetOutputEnumerable(CancellationToken ct);

        /// <summary>
        /// Return error stream of the shard
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        IAsyncEnumerable<TError> GetErrorsEnumerable(CancellationToken ct);

        /// <summary>
        /// Published message to the shard
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task PublishAsync(IEnumerable<TInput> messages, CancellationToken ct);

        Task RunAsync(CancellationToken ct);
    }
}