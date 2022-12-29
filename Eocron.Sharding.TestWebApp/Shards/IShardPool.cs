namespace Eocron.Sharding.TestWebApp.Shards
{
    public interface IShardPool<in TInput, TOutput, TError> : IShardProvider<TInput, TOutput, TError>
    {

    }
}