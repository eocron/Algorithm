using System.Threading.Channels;

namespace Eocron.Sharding
{
    public interface IShardOutputProvider<TOutput, TError>
    {
        /// <summary>
        ///     Errors coming from shard
        /// </summary>
        ChannelReader<ShardMessage<TError>> Errors { get; }

        /// <summary>
        ///     Outputs coming from shard
        /// </summary>
        ChannelReader<ShardMessage<TOutput>> Outputs { get; }
    }
}