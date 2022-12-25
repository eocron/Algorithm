namespace Eocron.Sharding.ProcessWatcher
{
    public interface IWatcherJob
    {
        Task RunAsync(CancellationToken stopToken);
    }
}