using System.Diagnostics;

namespace Eocron.Sharding.ProcessWatcher
{
    public static class ProcessHelper
    {
        public static async Task KillAllAsync(IEnumerable<Process> processes)
        {
            await Task.WhenAll(processes.Select(KillAsync)).ConfigureAwait(false);
        }

        public static async Task KillAsync(Process process)
        {
            await Task.Yield();
            try
            {
                if (!IsAlive(process))
                    return;

                process.Kill(true);
                await process.WaitForExitAsync().ConfigureAwait(false);
            }
            catch { }
        }

        public static bool IsAlive(Process process)
        {
            try
            {
                return !process.HasExited;
            }
            catch
            {
                return false;
            }
        }
    }
}
