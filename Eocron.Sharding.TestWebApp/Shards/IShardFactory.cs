namespace Eocron.Sharding.TestWebApp.Shards
{
    public interface IShardFactory<in TInput, TOutput, TError>
    {
        IShard<TInput, TOutput, TError> CreateNewShard(string id);
    }
}