using System;

namespace Eocron.Sharding.Processing
{
    public interface IProcessShard<in TInput, TOutput, TError> : IShard<TInput, TOutput, TError>
    {
        TimeSpan? TotalProcessorTime { get; }
        long? WorkingSet64 { get; }
    }
}