namespace Eocron.Sharding.Processing
{
    public interface IProcessShard<in TInput, TOutput, TError> : IShard<TInput, TOutput, TError>
    {
        bool TryGetProcessDiagnosticInfo(out ProcessDiagnosticInfo info);
    }
}