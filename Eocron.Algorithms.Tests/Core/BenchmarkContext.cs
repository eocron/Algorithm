using System.Diagnostics;
using System.Threading;

namespace Eocron.Algorithms.Tests.Core
{
    public class BenchmarkContext
    {
        public int TotalCount;
        public Stopwatch Stopwatch;
        public BenchmarkContext()
        {
            Stopwatch = new Stopwatch();
        }
        public void Increment(int count = 1)
        {
            Interlocked.Add(ref TotalCount, count);
        }

        public void Start()
        {
            TotalCount = 0;
            Stopwatch.Reset();
            Stopwatch.Start();
        }

        public void Stop()
        {
            Stopwatch.Stop();
        }
    }
}
