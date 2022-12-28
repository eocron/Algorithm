using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Sharding.Processing
{
    public interface IShardLifetimeManager
    {
        bool IsStopped();

        Task StopAsync(CancellationToken ct);

        bool TryStop();

        Task StartAsync(CancellationToken ct);

        Task RestartAsync(CancellationToken ct);
    }
}