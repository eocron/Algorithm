using System.Collections.Concurrent;
using System.Diagnostics;

namespace Eocron.Sharding.ChildProcessKiller
{
    internal class Program
    {
        private static readonly ConcurrentDictionary<int, Process> Children = new ConcurrentDictionary<int, Process>();
        private static volatile bool ShouldProcessInput = true;
        static async Task Main(string[] args)
        {
            var parentProcess = Process.GetProcessById(int.Parse(args[0]));
#pragma warning disable CS4014
            ProcessStdInput();
            ProcessPurgable();
#pragma warning restore CS4014
            await WaitUntilExited(parentProcess, TimeSpan.FromMilliseconds(300)).ConfigureAwait(false);
            ShouldProcessInput = false;
            await KillAllChildren();
        }

        static async Task KillAllChildren()
        {
            await Task.WhenAll(Children.Select(x => KillProcess(x.Value))).ConfigureAwait(false);
        }

        static async Task KillProcess(Process process)
        {
            try
            {
                if (process.HasExited)
                    return;

                process.Kill(true);
                await process.WaitForExitAsync().ConfigureAwait(false);
            }
            catch{}
        }

        static async Task ProcessStdInput()
        {
            await Task.Yield();

            while (ShouldProcessInput)
            {
                var line = Console.ReadLine();
                if(line == null)
                    continue;
                if(!int.TryParse(line, out var processId))
                    continue;
                TryAddChildProcess(processId);
            }
        }

        static bool TryAddChildProcess(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId); 
                Children.AddOrUpdate(processId, process, (_,x)=> x);
                return true;
            }
            catch
            {
                return false;
            }
        }

        static void PurgeCompleted()
        {
            var toDelete = Children.Where(x => x.Value.HasExited).Select(x => x.Key).ToList();
            foreach (var i in toDelete)
            {
                Children.Remove(i, out var _);
            }
        }

        static async Task ProcessPurgable()
        {
            await Task.Yield();

            while (ShouldProcessInput)
            {
                PurgeCompleted();
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }

        static async Task WaitUntilExited(Process process, TimeSpan interval)
        {
            while (!process.HasExited)
            {
                await Task.Delay(interval).ConfigureAwait(false);
            }
        }
    }
}