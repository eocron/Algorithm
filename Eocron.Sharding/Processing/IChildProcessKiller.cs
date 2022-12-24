using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Eocron.Sharding.Processing
{
    public interface IChildProcessKiller
    {
        ChannelWriter<int> ChildrenToWatch { get; }
        Task RunAsync(CancellationToken ct);
    }
}