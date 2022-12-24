using Eocron.Sharding.Processing;

namespace Eocron.Sharding.TestWebApp.Processing
{
    public sealed class ChildProcessKillerService : BackgroundService
    {
        private readonly IChildProcessKiller _killer;
        private readonly ILogger _logger;

        public ChildProcessKillerService(IChildProcessKiller killer, ILogger logger)
        {
            _killer = killer;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _killer.RunAsync(stoppingToken).ConfigureAwait(false);
            }
            catch(Exception e)
            {
                _logger.LogError(e, "Child process stopped running.");
            }
        }
    }
}
