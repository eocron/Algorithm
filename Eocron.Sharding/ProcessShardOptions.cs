using System;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

namespace Eocron.Sharding
{
    public class ProcessShardOptions
    {
        /// <summary>
        /// How much time to wait on graceful shutdown before process forcefully killed.
        /// If not set process killed immediately.
        /// </summary>
        public TimeSpan? GracefulStopTimeout { get; set; }

        /// <summary>
        /// How frequently process state is monitored in shard.
        /// Default: 100ms
        /// </summary>
        public TimeSpan? ProcessStatusCheckInterval { get; set; }

        public ProcessStartInfo StartInfo { get; set; }

        public DataflowBlockOptions OutputOptions { get; set; }

        public DataflowBlockOptions ErrorOptions { get; set; }

        public static TimeSpan DefaultProcessStatusCheckInterval = TimeSpan.FromMilliseconds(100);
        public static DataflowBlockOptions DefaultOutputOptions = new DataflowBlockOptions
        {
            EnsureOrdered = true,
            BoundedCapacity = -1,
        };
        public static DataflowBlockOptions DefaultErrorOptions = new DataflowBlockOptions
        {
            EnsureOrdered = false,
            BoundedCapacity = 10000,
        };
    }
}