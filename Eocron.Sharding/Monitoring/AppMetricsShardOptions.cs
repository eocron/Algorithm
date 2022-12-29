using System;
using System.Collections.Generic;

namespace Eocron.Sharding.Monitoring
{
    public class AppMetricsShardOptions
    {
        public Dictionary<string, string> Tags { get; set; }
        public TimeSpan CheckInterval { get; set; }
        public TimeSpan CheckTimeout { get; set; }
        public TimeSpan ErrorRestartInterval { get; set; }

        public AppMetricsShardOptions()
        {
            CheckInterval = TimeSpan.FromSeconds(5);
            CheckTimeout = TimeSpan.FromSeconds(5);
            ErrorRestartInterval = TimeSpan.FromSeconds(5);
        }
    }
}
