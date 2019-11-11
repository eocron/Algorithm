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
            return new FileCache<long>(Path.Combine(TestContext.CurrentContext.TestDirectory, "fileCache"),
                disableGc: true);
        }

        private string GetRandomPath()
        {
            var dir = Path.Combine(TestContext.CurrentContext.TestDirectory, "fileCacheTmp");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, Guid.NewGuid().ToString("N"));
        }

        private void DeleteRandomPaths()
        {
            Directory.Delete(Path.Combine(TestContext.CurrentContext.TestDirectory, "fileCacheTmp"), true);
        }

        private Stream GetRandomFile(long size)
        {
            return new TestStream(size, 42);
        }

        private void AssertNoCachedFiles(FileCache<long> cache)
        {
            Assert.IsEmpty(Directory.GetFiles(Path.Combine(cache.CurrentFolder, "cch")));
        }

        private void AssertHasCachedFiles(FileCache<long> cache)
        {
            Assert.IsNotEmpty(Directory.GetFiles(Path.Combine(cache.CurrentFolder, "cch")));
        }

        private void AssertNoFiles(string path)
        {
            if (Directory.Exists(path))
            {
                Assert.IsEmpty(Directory.GetFiles(path, "*", SearchOption.AllDirectories));
            }
        }

        private void AssertNoGarbageFiles(FileCache<long> cache)
        {
            AssertNoFiles(Path.Combine(cache.CurrentFolder, "bin"));
            AssertNoFiles(Path.Combine(cache.CurrentFolder, "tmp"));
            Assert.IsEmpty(Directory.GetDirectories(cache.BaseFolder).Where(x => x != cache.CurrentFolder));
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
            using (var cachedStream = await cache.GetStreamOrAddStreamAsync(123, async _ => GetRandomFile(fileSize),
                CancellationToken.None, null))
            {
            }

            sw.Stop();
            var heatup = sw.Elapsed;
            Console.WriteLine("Heatup: {0}", sw.Elapsed);

            //test
            sw.Restart();
            for (int i = 0; i < iterCount; i++)
            {
                ms = new MemoryStream();
                using (var cachedStream = await cache.GetStreamOrAddStreamAsync(123, async _ => GetRandomFile(fileSize),
                    CancellationToken.None, null))
                {
                }
            }

            sw.Stop();
            var accessTime = new TimeSpan(sw.Elapsed.Ticks / iterCount);
            Console.WriteLine("Cache access time: {0}", accessTime);
            Console.WriteLine("Total access time: {0}", sw.Elapsed);

            Assert.Greater(heatup, accessTime);


            await cache.GarbageCollectAsync(CancellationToken.None);

            AssertNoGarbageFiles(cache);
        }

        private async Task<byte[]> FullReadAsync(IFileCache<long> cache, long key)
        {
            var ms = new MemoryStream();
            using (var cached = await cache.TryGetStreamAsync(key, CancellationToken.None))
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

            await cache.GarbageCollectAsync(CancellationToken.None);

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

            await cache.GarbageCollectAsync(CancellationToken.None);

            data = await FullReadAsync(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);

            AssertNoGarbageFiles(cache);
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

            await cache.GarbageCollectAsync(CancellationToken.None);

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

            await cache.GarbageCollectAsync(CancellationToken.None);

            data = await FullReadAsync(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);

            AssertNoGarbageFiles(cache);
        }

        [Test]
        public async Task AbsoluteExpiration()
        {
            var cache = CreateCache();
            var fileSize = 1 * 1024;

            var data = await FullReadAsync(cache, 123);
            Assert.IsNull(data);
            //expired long time ago
            await cache.AddOrUpdateStreamAsync(123, GetRandomFile(fileSize), CancellationToken.None,
                CacheExpirationPolicy.AbsoluteUtc(DateTime.MinValue));

            data = await FullReadAsync(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);

            await cache.GarbageCollectAsync(CancellationToken.None); //here is expired one collected

            data = await FullReadAsync(cache, 123);
            Assert.IsNull(data);

            AssertNoGarbageFiles(cache);
        }

        [Test]
        public async Task LockedForReadFileGc()
        {
            var cache = CreateCache();
            var fileSize = 10;
            var data = await FullReadAsync(cache, 123);
            Assert.IsNull(data);
            await cache.AddOrUpdateStreamAsync(123, GetRandomFile(fileSize), CancellationToken.None,
                CacheExpirationPolicy.AbsoluteUtc(DateTime.MinValue));
            data = await FullReadAsync(cache, 123);
            Assert.IsNotNull(data);

            var ms = new MemoryStream();
            using (var s = await cache.TryGetStreamAsync(123, CancellationToken.None))
            {
                await cache.GarbageCollectAsync(CancellationToken.None); //item is expired but will not be collected.
                await s.CopyToAsync(ms);
            }

            data = await FullReadAsync(cache, 123);
            Assert.IsNull(data);

            AssertHasCachedFiles(cache);

            await cache.GarbageCollectAsync(CancellationToken.None); //trash collected if not collected on read.

            AssertNoCachedFiles(cache);
            AssertNoGarbageFiles(cache);
        }

        [Test]
        public async Task Stress()
        {
            try
            {
                var cache = CreateCache();
                var fileSize = 1 * 1024;
                var data = await FullReadAsync(cache, 123);
                Assert.IsNull(data);
                var tasks = Enumerable.Range(0, 100).Select(async i =>
                {
                    var filePath = GetRandomPath();
                    await cache.GetFileOrAddStreamAsync(123, async x => GetRandomFile(fileSize), CancellationToken.None, filePath,
                        CacheExpirationPolicy.AbsoluteUtc(DateTime.MinValue));
                    await cache.GarbageCollectAsync(CancellationToken.None);
                    await AssertFile(filePath, fileSize);
                }).ToArray();

                await Task.WhenAll(tasks);

                await cache.GarbageCollectAsync(CancellationToken.None); //trash collected if not collected on read.

                AssertNoCachedFiles(cache);
                AssertNoGarbageFiles(cache);
            }
            finally
            {
                DeleteRandomPaths();
            }
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
            await cache.AddOrUpdateStreamAsync(123, GetRandomFile(fileSize), CancellationToken.None,
                CacheExpirationPolicy.SlidingUtc(slide));

            for (int i = 0; i < calls; i++)
            {
                data = await FullReadAsync(cache, 123);
                Assert.IsNotNull(data);
                Assert.AreEqual(fileSize, data.Length);
                await cache.GarbageCollectAsync(CancellationToken.None); //here is expired one collected
                await Task.Delay(part);
            }

            await Task.Delay(new TimeSpan(2 * slide.Ticks));
            await cache.GarbageCollectAsync(CancellationToken.None); //here is expired one collected

            data = await FullReadAsync(cache, 123);
            Assert.IsNull(data);

            AssertNoGarbageFiles(cache);
        }

        [Test]
        public async Task CancellationSpam()
        {
            var cache = CreateCache();
            var fileSize = 1L * 1024 * 1024 * 1024 * 1024; //1 TB

            var calls = 100;


            for (int i = 0; i < calls; i++)
            {
                using (var cts = new CancellationTokenSource())
                {
                    cts.CancelAfter(10);
                    try
                    {
                        await cache.AddOrUpdateStreamAsync(123, GetRandomFile(fileSize), cts.Token, null);
                    }
                    catch (OperationCanceledException)
                    {
                        //good.
                    }
                }
            }

            await cache.GarbageCollectAsync(CancellationToken.None);

            AssertNoGarbageFiles(cache);
        }

        [Test]
        public async Task PerformanceCopyOfFile()
        {
            try
            {

                var cache = CreateCache();
                var fileSize = 10L * 1024 * 1024;
                var iterCount = 10;

                Console.WriteLine("File size: {0:F1}mb", fileSize / (1024f * 1024f));
                Console.WriteLine("Iter count: {0}", iterCount);
                Stopwatch sw = Stopwatch.StartNew();
                //heat up
                var targetFilePath = GetRandomPath();
                await cache.GetFileOrAddStreamAsync(123, async _ => GetRandomFile(fileSize), CancellationToken.None,
                    targetFilePath, null);
                sw.Stop();
                await AssertFile(targetFilePath, fileSize);


                var heatup = sw.Elapsed;
                Console.WriteLine("Heatup: {0}", sw.Elapsed);


                //test
                sw.Restart();
                for (int i = 0; i < iterCount; i++)
                {
                    targetFilePath = GetRandomPath();
                    await cache.GetFileOrAddStreamAsync(123, async _ => GetRandomFile(fileSize), CancellationToken.None,
                        targetFilePath, null);
                }

                sw.Stop();
                var accessTime = new TimeSpan(sw.Elapsed.Ticks / iterCount);
                Console.WriteLine("Cache access time: {0}", accessTime);
                Console.WriteLine("Total access time: {0}", sw.Elapsed);

                Assert.Greater(heatup, accessTime);


                await cache.GarbageCollectAsync(CancellationToken.None);

                AssertNoGarbageFiles(cache);
            }
            finally
            {
                DeleteRandomPaths();
            }
        }

        private async Task AssertFile(string filePath, long fileSize)
        {
            Assert.IsTrue(File.Exists(filePath));
            Assert.AreEqual(fileSize, new FileInfo(filePath).Length);
            using (var s = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                s.ReadByte();
            }
        }

        [Test]
        public async Task LockedForReadHardlinkGc()
        {
            try
            {
                var cache = CreateCache();
                var fileSize = 1 * 1024 * 1024; // 1 mb

                await cache.AddOrUpdateStreamAsync(123, GetRandomFile(fileSize), CancellationToken.None,
                    CacheExpirationPolicy.AbsoluteUtc(DateTime.MinValue));
                var data = await FullReadAsync(cache, 123);
                Assert.IsNotNull(data);


                var targetFilePath = GetRandomPath();
                Assert.IsTrue(await cache.TryGetFileAsync(123, CancellationToken.None, targetFilePath));
                try
                {
                    await AssertFile(targetFilePath, fileSize);
                    AssertHasCachedFiles(cache);

                    using (var s = File.OpenRead(targetFilePath))
                    {
                        var bytes = new byte[8 * 1024];
                        Assert.IsTrue(await s.ReadAsync(bytes, 0, bytes.Length) > 0);
                        await cache.GarbageCollectAsync(CancellationToken.None);
                    }

                    await AssertFile(targetFilePath, fileSize);//item is expired, collected, but available to user

                    data = await FullReadAsync(cache, 123);
                    Assert.IsNull(data);

                    await cache.GarbageCollectAsync(CancellationToken.None); //trash collected if not collected on read.

                    AssertNoCachedFiles(cache);
                    AssertNoGarbageFiles(cache);

                    await AssertFile(targetFilePath, fileSize);
                }
                finally
                {
                    File.Delete(targetFilePath);
                }
            }
            catch
            {
                DeleteRandomPaths();
            }
        }

    }
}
