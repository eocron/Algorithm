using Eocron.Algorithms;
using NTests.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace NTests
{
    [TestFixture, Category("Performance"), Explicit]
    public class ByteArrayEqualityComparerPerformanceTests
    {

        public List<byte[]> ArrayPool;
        public IEqualityComparer<ArraySegment<byte>> Comparer;

        [SetUp]
        public void SetUp()
        {
            ArrayPool = new List<byte[]>();
            var rnd = new Random();
            int size = 10 * 1024 * 1024;
            int count = 10;
            var tmp = new byte[size];
            rnd.NextBytes(tmp);
            for (int i = 0; i < count; i++)
            {
                var tmp2 = new byte[size];
                Array.Copy(tmp, tmp2, tmp.Length);
                ArrayPool.Add(tmp2);
            }
            Comparer = new ByteArrayEqualityComparer();
        }

        [Test]
        public void PerformanceGetHashCode()
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            Benchmark.InfiniteMeasure((ctx) =>
            {
                int count = 200000;
                for (int i = 0; i < count; i++)
                {
                    Comparer.GetHashCode(ArrayPool[i % ArrayPool.Count]);
                }
                ctx.Increment(count);
            }, cts.Token);
        }

        [Test]
        public void PerformanceEquals()
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            Benchmark.InfiniteMeasure((ctx) =>
            {
                int count = 1000;
                for (int i = 0; i < count; i++)
                {
                    Comparer.Equals(ArrayPool[i % ArrayPool.Count], ArrayPool[(i + 1) % ArrayPool.Count]);
                }
                ctx.Increment(count);
            }, cts.Token);
        }
    }
}
