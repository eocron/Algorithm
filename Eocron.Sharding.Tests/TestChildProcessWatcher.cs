using System.Threading.Channels;
using Eocron.Sharding.Processing;

namespace Eocron.Sharding.Tests
{
    public class TestChildProcessWatcher : IChildProcessWatcher
    {
        private readonly Channel<int> _channel;

        public TestChildProcessWatcher()
        {
            _channel = Channel.CreateUnbounded<int>();
        }

        public Channel<int> ChildrenToWatch => _channel;
    }
}