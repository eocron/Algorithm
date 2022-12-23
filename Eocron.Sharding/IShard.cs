using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Eocron.Sharding
{
    public interface IShard<in TInput, TOutput, TError> : IDisposable
    {
        /// <summary>
        /// Checks if shard is ready for publish
        /// </summary>
        /// <returns></returns>
        bool IsReadyForPublish();

        IReceivableSourceBlock<TOutput> Outputs { get; }

        IReceivableSourceBlock<TError> Errors { get; }

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