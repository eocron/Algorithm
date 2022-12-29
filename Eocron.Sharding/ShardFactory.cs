namespace Eocron.Sharding
{
    internal sealed class ShardFactory<TInput, TOutput, TError> : IShardFactory<TInput, TOutput, TError>
    {
        private readonly ShardBuilder<TInput, TOutput, TError> _builder;

        public ShardFactory(ShardBuilder<TInput, TOutput, TError> builder)
        {
            _builder = builder;
        }

        public IShard<TInput, TOutput, TError> CreateNewShard(string id)
        {
            return _builder.Build(id);
        }
    }
}