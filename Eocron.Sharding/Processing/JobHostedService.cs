using System.Threading;
using System.Threading.Tasks;
using Eocron.Sharding.Jobs;
using Microsoft.Extensions.Hosting;

namespace Eocron.Sharding.Processing
{
    public sealed class JobHostedService : BackgroundService
    {
        private readonly IJob _job;

        public JobHostedService(IJob job)
        {
            _job = job;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _job.RunAsync(stoppingToken).ConfigureAwait(false);
        }
    }
}
