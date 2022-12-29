using System;
using System.Diagnostics;
using System.Threading.Channels;

namespace Eocron.Sharding.Processing
{
    public class ProcessShardOptions
    {
        public BoundedChannelOptions ErrorOptions { get; set; }

        public BoundedChannelOptions OutputOptions { get; set; }

        public ProcessStartInfo StartInfo { get; set; }

        /// <summary>
        ///     How much time to wait on graceful shutdown before process forcefully killed.
        ///     If not set process killed immediately.
        /// </summary>
        public TimeSpan? GracefulStopTimeout { get; set; }

        /// <summary>
        ///     How frequently process state is monitored in shard.
        ///     Default: 100ms
        /// </summary>
        public TimeSpan ProcessStatusCheckInterval { get; set; }

        public TimeSpan ErrorRestartInterval { get; set; }

        public TimeSpan SuccessRestartInterval { get; set; }
        
        public ProcessShardOptions()
        {
            ProcessStatusCheckInterval = TimeSpan.FromMilliseconds(100);
            ErrorOptions = new(10000)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            };
            OutputOptions = new(10000)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            ErrorRestartInterval = TimeSpan.Zero;
            SuccessRestartInterval = TimeSpan.Zero;
        }
    }
}