using Eocron.Sharding.Jobs;

namespace Eocron.Sharding.Processing
{
    public interface IProcessJob<in TInput, TOutput, TError> :
        IShard,
        IShardOutputProvider<TOutput, TError>,
        IShardInputManager<TInput>,
        IProcessDiagnosticInfoProvider,
        IJob
    {

    }
}