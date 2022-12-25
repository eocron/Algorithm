namespace Eocron.Sharding.Processing
{
    public interface IProcessDiagnosticInfoProvider
    {
        bool TryGetProcessDiagnosticInfo(out ProcessDiagnosticInfo info);
    }
}