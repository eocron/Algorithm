using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Eocron.Sharding.Processing
{
    public sealed class ChildProcessKiller : IChildProcessKiller
    {
        private readonly ILogger _logger;
        private readonly ProcessStartInfo _startInfo;
        private readonly Channel<int> _channel;
        public ChildProcessKiller(ILogger logger)
        {
            _logger = logger;
            _startInfo = new ProcessStartInfo
            {
                FileName = "killer/killer.exe",
                Arguments = Process.GetCurrentProcess().Id.ToString()
            }.ConfigureAsService();
            _channel = Channel.CreateUnbounded<int>();
        }

        public ChannelWriter<int> ChildrenToWatch => _channel.Writer;

        public async Task RunAsync(CancellationToken ct)
        {
            using var process = Process.Start(_startInfo);
            while (!process.HasExited)
            {
                while (_channel.Reader.TryRead(out var childId))
                {
                    await process.StandardInput.WriteLineAsync(childId.ToString());
                    _logger.LogDebug("Child process now monitored for kill {process_id} on parent exit.", childId);
                }
                await Task.Delay(TimeSpan.FromMilliseconds(300), ct).ConfigureAwait(false);
            }
        }
    }
}
