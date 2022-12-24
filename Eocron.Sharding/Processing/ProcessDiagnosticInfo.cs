using System;

namespace Eocron.Sharding.Processing
{
    public class ProcessDiagnosticInfo
    {
        public TimeSpan TotalProcessorTime { get; set; }
        public long WorkingSet64 { get; set; }
        public long PrivateMemorySize64 { get; set; }
        public string ModuleName { get; set; }
    }
}