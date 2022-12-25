using System;

namespace Eocron.Sharding.Processing
{
    public class ProcessDiagnosticInfo
    {
        public long PrivateMemorySize64 { get; set; }
        public long WorkingSet64 { get; set; }
        public string ModuleName { get; set; }
        public TimeSpan TotalProcessorTime { get; set; }
    }
}