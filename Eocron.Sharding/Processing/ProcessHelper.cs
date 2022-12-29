using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Eocron.Sharding.Processing
{
    internal static class ProcessHelper
    {
        public static T DefaultIfNotFound<T>(Process process, Func<Process, T> processAccessor, T @default)
        {
            if (process == null)
                return @default;
            try
            {
                return processAccessor(process);
            }
            catch (InvalidOperationException)
            {
                return @default;
            }
            catch (Win32Exception)
            {
                return @default;
            }
        }

        public static bool IsDead(Process process)
        {
            return DefaultIfNotFound(process, x => x.HasExited, true);
        }

        public static bool IsAlive(Process process)
        {
            return !IsDead(process);
        }

        public static int? GetId(Process process)
        {
            return DefaultIfNotFound(process, x => (int?)x.Id, null);
        }

        public static int? GetExitCode(Process process)
        {
            return DefaultIfNotFound(process, x => (int?)x.ExitCode, null);
        }
    }
}
