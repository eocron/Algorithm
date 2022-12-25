using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Eocron.Sharding.ProcessWatcher
{
    public sealed class ProcessWatcher : IWatcherJob
    {
        private readonly ILogger _logger;
        private readonly TimeSpan _purgeInterval;
        private readonly ConcurrentDictionary<int, Process> _watched = new ConcurrentDictionary<int, Process>();

        public IEnumerable<Process> CurrentProcesses => _watched.Values;
        public ProcessWatcher(ILogger logger, TimeSpan purgeInterval)
        {
            _logger = logger;
            _purgeInterval = purgeInterval;
        }

        public void Add(int processId)
        {
            var process = Process.GetProcessById(processId);
            if (!ProcessHelper.IsAlive(process))
            {
                _logger.LogInformation("Stop watching {process_id}", processId);
                return;
            }

            _watched.AddOrUpdate(processId, process, (_, x) => x);
            _logger.LogInformation("Start watching {process_id}", processId);
        }
        
        public async Task RunAsync(CancellationToken stopToken)
        {
            await Task.Yield();
            while (!stopToken.IsCancellationRequested)
            {
                var dead = _watched.Where(x => !ProcessHelper.IsAlive(x.Value)).Select(x=> x.Key).ToList();
                foreach (var id in dead)
                {
                    _watched.TryRemove(id, out var _);
                    _logger.LogInformation("Stop watching {process_id}", id);
                }

                try
                {
                    await Task.Delay(_purgeInterval, stopToken).ConfigureAwait(false);
                }
                catch
                {
                    break;
                }
            }
        }


    }
}