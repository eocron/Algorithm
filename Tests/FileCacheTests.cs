using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Algorithm.FileCache;
using NUnit.Framework;
namespace Tests
{
    [TestFixture]
    public class FileCacheTests
    {

        private FileCache<long> CreateCache()
        {
            return new FileCache<long>(Path.Combine(TestContext.CurrentContext.TestDirectory, "fileCache"));
        }

        private Stream GetRandomFile(long size)
        {
            return new TestStream(size, 42);
        }

        [Test]
        public async Task Performance()
        {
            var cache = CreateCache();
            var fileSize = 10 * 1024 * 1024;
            var iterCount = 10;

            Console.WriteLine("File size: {0:F1}mb", fileSize / (1024f * 1024f));
            Console.WriteLine("Iter count: {0}", iterCount);
            Stopwatch sw = Stopwatch.StartNew();
            //heat up
            var ms = new MemoryStream();
            using (var cachedStream = await cache.GetStreamOrAddStreamAsync(123, async _ => GetRandomFile(fileSize), CancellationToken.None, null))
            {
                await cachedStream.CopyToAsync(ms);
            }

            sw.Stop();
            var heatup = sw.Elapsed;
            Console.WriteLine("Heatup: {0}", sw.Elapsed);

            //test
            sw.Restart();
            for (int i = 0; i < iterCount; i++)
            {
                ms = new MemoryStream();
                using (var cachedStream = await cache.GetStreamOrAddStreamAsync(123, async _ => GetRandomFile(fileSize), CancellationToken.None, null))
                {
                    await cachedStream.CopyToAsync(ms);
                }
            }

            sw.Stop();
            var accessTime = new TimeSpan(sw.Elapsed.Ticks / iterCount);
            Console.WriteLine("Cache access time: {0}", accessTime);
            Console.WriteLine("Total access time: {0}", sw.Elapsed);

            Assert.Greater(heatup, accessTime);


            await cache.GarbageCollect(CancellationToken.None);

            Assert.IsEmpty(Directory.GetDirectories(cache.BaseFolder).Where(x => x != cache.CurrentFolder));
        }

        private async Task<byte[]> FullReadAsync(IFileCache<long> cache, long key)
        {
            var ms = new MemoryStream();
            using (var cached = await cache.TryGetStream(123, CancellationToken.None))
            {
                if (cached == null)
                    return null;
                await cached.CopyToAsync(ms);
            }

            return ms.ToArray();
        }

        [Test]
        public async Task Invalidate()
        {
            var cache = CreateCache();
            var fileSize = 1 * 1024;

            var data = await FullReadAsync(cache, 123);
            Assert.IsNull(data);

            await cache.AddOrUpdateStreamAsync(123, GetRandomFile(fileSize), CancellationToken.None, null);

            data = await FullReadAsync(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);

            await cache.GarbageCollect(CancellationToken.None);

            data = await FullReadAsync(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);
            //
            await cache.InvalidateAsync(CancellationToken.None);
            //
            data = await FullReadAsync(cache, 123);
            Assert.IsNull(data);

            await cache.AddOrUpdateStreamAsync(123, GetRandomFile(fileSize), CancellationToken.None, null);

            data = await FullReadAsync(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);

            await cache.GarbageCollect(CancellationToken.None);

            data = await FullReadAsync(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);

            Assert.IsEmpty(Directory.GetDirectories(cache.BaseFolder).Where(x => x != cache.CurrentFolder));
        }

        [Test]
        public async Task InvalidateKey()
        {
            var cache = CreateCache();
            var fileSize = 1 * 1024;

            var data = await FullReadAsync(cache, 123);
            Assert.IsNull(data);

            await cache.AddOrUpdateStreamAsync(123, GetRandomFile(fileSize), CancellationToken.None, null);

            data = await FullReadAsync(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);

            await cache.GarbageCollect(CancellationToken.None);

            data = await FullReadAsync(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);
            //
            await cache.InvalidateAsync(123, CancellationToken.None);
            //
            data = await FullReadAsync(cache, 123);
            Assert.IsNull(data);

            await cache.AddOrUpdateStreamAsync(123, GetRandomFile(fileSize), CancellationToken.None, null);

            data = await FullReadAsync(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);

            await cache.GarbageCollect(CancellationToken.None);

            data = await FullReadAsync(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);

            Assert.IsEmpty(Directory.GetDirectories(cache.BaseFolder).Where(x => x != cache.CurrentFolder));
        }

        [Test]
        public async Task AbsoluteExpiration()
        {
            var cache = CreateCache();
            var fileSize = 1 * 1024;

            var data = await FullReadAsync(cache, 123);
            Assert.IsNull(data);
            //expired long time ago
            await cache.AddOrUpdateStreamAsync(123, GetRandomFile(fileSize), CancellationToken.None, CacheExpirationPolicy.AbsoluteUtc(DateTime.MinValue));

            data = await FullReadAsync(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);

            await cache.GarbageCollect(CancellationToken.None);//here is expired one collected

            data = await FullReadAsync(cache, 123);
            Assert.IsNull(data);

            Assert.IsEmpty(Directory.GetDirectories(cache.BaseFolder).Where(x => x != cache.CurrentFolder));
        }

        [Test]
        public async Task LockedForReadFileGc()
        {
            var cache = CreateCache();
            var fileSize = 10;
            var data = await FullReadAsync(cache, 123);
            Assert.IsNull(data);
            await cache.AddOrUpdateStreamAsync(123, GetRandomFile(fileSize), CancellationToken.None, CacheExpirationPolicy.AbsoluteUtc(DateTime.MinValue));
            data = await FullReadAsync(cache, 123);
            Assert.IsNotNull(data);

            var ms = new MemoryStream();
            using(var s = await cache.TryGetStream(123, CancellationToken.None))
            {
                await cache.GarbageCollect(CancellationToken.None);//item is expired but will not be collected.
                await s.CopyToAsync(ms);
            }

            data = await FullReadAsync(cache, 123);
            Assert.IsNull(data);

            Assert.IsNotEmpty(Directory.GetFiles(cache.CurrentFolder, "*", SearchOption.AllDirectories));

            await cache.GarbageCollect(CancellationToken.None);//trash collected if not collected on read.

            Assert.IsEmpty(Directory.GetFiles(cache.CurrentFolder, "*", SearchOption.AllDirectories));
        }

        [Test]
        public async Task SlidingExpiration()
        {
            var cache = CreateCache();
            var fileSize = 10;

            var slide = TimeSpan.FromMilliseconds(200);
            var calls = 10;

            var part = new TimeSpan(2 * slide.Ticks / calls);

            var data = await FullReadAsync(cache, 123);
            Assert.IsNull(data);
            //expired long time ago
            await cache.AddOrUpdateStreamAsync(123, GetRandomFile(fileSize), CancellationToken.None, CacheExpirationPolicy.SlidingUtc(slide));

            for (int i = 0; i < calls; i++)
            {
                data = await FullReadAsync(cache, 123);
                Assert.IsNotNull(data);
                Assert.AreEqual(fileSize, data.Length);
                await cache.GarbageCollect(CancellationToken.None);//here is expired one collected
                await Task.Delay(part);
            }

            await Task.Delay(new TimeSpan(2 * slide.Ticks));
            await cache.GarbageCollect(CancellationToken.None);//here is expired one collected

            data = await FullReadAsync(cache, 123);
            Assert.IsNull(data);

            Assert.IsEmpty(Directory.GetDirectories(cache.BaseFolder).Where(x => x != cache.CurrentFolder));
        }
    }
}
