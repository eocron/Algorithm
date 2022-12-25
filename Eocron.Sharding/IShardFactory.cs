namespace Eocron.Sharding
{
    public interface IShardFactory<in TInput, TOutput, TError>
    {
        IShard<TInput, TOutput, TError> CreateNewShard(string id);
    }
}