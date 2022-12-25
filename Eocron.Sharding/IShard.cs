using Eocron.Sharding.Jobs;

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
        ICancellationManager,
        IJob
    {

    }
}