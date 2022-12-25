using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Eocron.Sharding.Jobs;
using Microsoft.Extensions.Logging;

namespace Eocron.Sharding.Processing
{
    public sealed class ChildProcessWatcher : IChildProcessWatcher, IJob
    {
        public ChildProcessWatcher(ILogger logger, TimeSpan? watchInterval = null, ProcessStartInfo startInfo = null)
        {
            _logger = logger;
            _watchInterval = watchInterval ?? TimeSpan.FromMilliseconds(300);
            _startInfo = startInfo ?? new ProcessStartInfo
            {
                FileName = "Tools/ProcessWatcher/process_watcher.exe"
            }.ConfigureAsService();

            _startInfo.Arguments = $"--ParentProcessId {Process.GetCurrentProcess().Id}";
            ChildrenToWatch = Channel.CreateUnbounded<int>();
        }

        public void Dispose()
        {
        }

        public async Task RunAsync(CancellationToken ct)
        {
            await Task.Yield();
            using var process = Process.Start(_startInfo);
            while (!process.HasExited)
            {
                while (ChildrenToWatch.Reader.TryRead(out var childId))
                {
                    await process.StandardInput.WriteLineAsync($"--ProcessId {childId}");
                    _logger.LogDebug("Watching {process_id}", childId);
                }

                await Task.Delay(_watchInterval, ct).ConfigureAwait(false);
            }
        }

        public Channel<int> ChildrenToWatch { get; }

        private readonly ILogger _logger;
        private readonly ProcessStartInfo _startInfo;
        private readonly TimeSpan _watchInterval;
    }
}