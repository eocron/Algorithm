using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Sharding.Processing
{
    public interface IShardLifetimeManager
    {
        Task<bool> IsStoppedAsync(CancellationToken ct);

        Task StopAsync(CancellationToken ct);

        Task<bool> TryStopAsync(CancellationToken ct);

        Task StartAsync(CancellationToken ct);

        Task RestartAsync(CancellationToken ct);
    }
}