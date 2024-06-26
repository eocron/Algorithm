using System.Threading;
using System.Threading.Tasks;

namespace Eocron.ProxyHost
{
    public interface IProxy
    {
        Task StartAsync(CancellationToken ct);

        Task StopAsync(CancellationToken ct);
    }
}