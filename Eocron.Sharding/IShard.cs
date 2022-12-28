using Eocron.Sharding.Jobs;
using Eocron.Sharding.Processing;

namespace Eocron.Sharding
{
    public interface IShard
    {
        string Id { get; }
    }

    public interface IShard<in TInput, TOutput, TError> :
        IShard,
        IShardInputManager<TInput>,
        IShardOutputProvider<TOutput, TError>,
        IShardLifetimeManager,
        IJob
    {
    }
}