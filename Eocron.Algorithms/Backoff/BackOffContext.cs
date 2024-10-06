using System;
using System.Reflection;

namespace Eocron.Algorithms.Backoff
{
    public class BackOffContext
    {
        public int N { get; set; }
        
        public Exception Exception { get; set; }
        
        public MethodInfo MethodInfo { get; set; }
        
        public object Target { get; set; }
    }
}