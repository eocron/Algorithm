namespace Eocron.Sharding.TestWebApp.Shards
{
    public interface IShardProvider<in TInput, TOutput, TError>
    {
        IEnumerable<IShard<TInput, TOutput, TError>> GetAllShards();

        IShard<TInput, TOutput, TError> FindShardById(string id);
    }
}