using Eocron.Algorithms;
using NTests.Core;
using NUnit.Framework;
using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NTests
{
    [TestFixture, Category("Performance"), Explicit]
    public class StreamHashPerformanceTests
    {
        private string _filePath;
        [SetUp]
        public async Task SetUp()
        {
            var size = 1024 * 1024 * 1024;
            var rnd = new Random(42);
            var bytes = ArrayPool<byte>.Shared.Rent(8 * 1024);
            var tmpFilePath = Path.GetTempFileName();
            _filePath = tmpFilePath;
            using(var fs = File.OpenWrite(tmpFilePath))
            {
                int len = size;
                while(len > 0)
                {
                    rnd.NextBytes(bytes);
                    var writeCount = Math.Min(bytes.Length, len);
                    await fs.WriteAsync(bytes, 0, writeCount);
                    len -= writeCount;
                }
            }
        }
        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
        }



        [Test]
        public async Task CalculateSpeed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            await Benchmark.InfiniteMeasureAsync(async (ctx) =>
            {
                const int count = 100;
                for (var i = 0; i < count; i++)
                {
                    using (var fs = File.OpenRead(_filePath))
                    {
                        var hash = await fs.GetHashCodeAsync(cts.Token);
                    }
                }
                ctx.Increment(count);
            }, cts.Token);
        }
    }
}
