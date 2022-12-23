using System;
using System.Diagnostics;
using System.Threading.Channels;

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

        public BoundedChannelOptions OutputOptions { get; set; }

        public BoundedChannelOptions ErrorOptions { get; set; }

        public static TimeSpan DefaultProcessStatusCheckInterval = TimeSpan.FromMilliseconds(100);
        public static BoundedChannelOptions DefaultOutputOptions = new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        public static BoundedChannelOptions DefaultErrorOptions = new BoundedChannelOptions(10000)
            { FullMode = BoundedChannelFullMode.DropOldest };
    }
}