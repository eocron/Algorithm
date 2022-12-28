using System.Diagnostics;

namespace Eocron.Sharding.Processing
{
    internal static class ProcessHelper
    {
        public static bool IsDead(Process process)
        {
            if (process == null)
                return true;
            try
            {
                return process.HasExited;
            }
            catch
            {
                return true;
            }
        }

        public static bool IsAlive(Process process)
        {
            return !IsDead(process);
        }

        public static int? GetId(Process process)
        {
            try
            {
                return process.Id;
            }
            catch
            {
                return null;
            }
        }

        public static int? GetExitCode(Process process)
        {
            try
            {
                return process.ExitCode;
            }
            catch
            {
                return null;
            }
        }
    }
}
