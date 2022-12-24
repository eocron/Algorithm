using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Eocron.Sharding
{
    public interface IShard<in TInput, TOutput, TError> : IDisposable
    {
        string Id { get; }
        /// <summary>
        /// Checks if shard is ready for publish
        /// </summary>
        /// <returns></returns>
        bool IsReadyForPublish();

        /// <summary>
        /// Outputs coming from shard
        /// </summary>
        ChannelReader<ShardMessage<TOutput>> Outputs { get; }

        /// <summary>
        /// Errors coming from shard
        /// </summary>
        ChannelReader<ShardMessage<TError>> Errors { get; }

        /// <summary>
        /// Publish messages to the shard
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task PublishAsync(IEnumerable<TInput> messages, CancellationToken ct);

        Task RunAsync(CancellationToken ct);
    }
}