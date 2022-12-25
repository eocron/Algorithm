using System.Threading.Channels;

namespace Eocron.Sharding.Processing
{
    public interface IChildProcessWatcher
    {
        Channel<int> ChildrenToWatch { get; }
    }
}