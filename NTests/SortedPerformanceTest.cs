using NUnit.Framework;
using System;
using System.Linq;
using Eocron.Algorithms;
using NTests.Core;
using System.Threading;

namespace NTests
{
    [TestFixture]
    public class SortedPerformanceTest
    {
        [Test, Explicit]
        public void Perf()
        {
            var rnd = new Random();
            var arraySize = 1000000;
            var all = Enumerable.Range(0, arraySize).Select(x => rnd.Next()).OrderBy(x=> x).ToArray();
            var first = all[0];
            var last = all[all.Length - 1];
            var middle = all[all.Length / 2];
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            Benchmark.InfiniteMeasure((ctx) =>
            {
                const int count = 1000;
                for (int i = 0; i < count; i++)
                {
                    all.BinarySearchIndexOf(first);
                    all.BinarySearchIndexOf(middle);
                    all.BinarySearchIndexOf(last);
                }
                ctx.Increment(2* count);
            }, cts.Token);
        }
    }
}
