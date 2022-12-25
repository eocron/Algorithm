using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Eocron.Sharding.ProcessWatcher
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var logger = new ConsoleLogger();

            try
            {
                var config = new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .Build();

                var parentProcess = Process.GetProcessById(int.Parse(config["ParentProcessId"]));

                var processWatcher = new ProcessWatcher(logger, TimeSpan.FromSeconds(5));
                var inputHandler = new InputHandler(logger, (cfg, ct) =>
                {
                    var id = int.Parse(cfg["ProcessId"]);
                    processWatcher.Add(id);
                    return Task.CompletedTask;
                });

                using var cts = new CancellationTokenSource();
                var jobs = Task.WhenAll(
                    inputHandler.RunAsync(cts.Token),
                    processWatcher.RunAsync(cts.Token));

                await WaitWhileAsync(() => ProcessHelper.IsAlive(parentProcess), TimeSpan.FromMilliseconds(300))
                    .ConfigureAwait(false);
                cts.Cancel();
                await jobs;
                var processes = processWatcher.CurrentProcesses.ToList();
                await ProcessHelper.KillAllAsync(processes);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unhandled exception on watcher");
                throw;
            }
        }


        private static async Task WaitWhileAsync(Func<bool> check, TimeSpan? interval = null)
        {
            interval ??= TimeSpan.FromMilliseconds(300);
            while (check()) await Task.Delay(interval.Value).ConfigureAwait(false);
        }
    }
}