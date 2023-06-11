using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Eocron.Algorithms.FileCache;
using Eocron.Algorithms.Tests.Core;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests
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
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "fileCacheTmp");
            Directory.Delete(path, true);
        }

        private Stream GetRandomFile(long size)
        {
            return new AssertStream(size, 42);
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
            if (Directory.Exists(path)) Assert.IsEmpty(Directory.GetFiles(path, "*", SearchOption.AllDirectories));
        }

        private void AssertNoGarbageFiles(FileCache<long> cache)
        {
            AssertNoFiles(Path.Combine(cache.CurrentFolder, "bin"));
            AssertNoFiles(Path.Combine(cache.CurrentFolder, "tmp"));
            Assert.IsEmpty(Directory.GetDirectories(cache.BaseFolder).Where(x => x != cache.CurrentFolder));
        }

        [Test]
        public void StreamDisposed()
        {
            var cache = CreateCache();
            var fileSize = 10 * 1024 * 1024;

            var stream = new AssertStream(fileSize, 42);
            using (var cachedStream = cache.GetStreamOrAddStream(123, _ => stream,
                       CancellationToken.None, null))
            {
                cachedStream.ReadByte();
            }

            Assert.IsTrue(stream.Closed);
            Assert.IsTrue(stream.Disposed);
        }

        [Test]
        public void StreamDisposed2()
        {
            var cache = CreateCache();
            var fileSize = 10 * 1024 * 1024;

            var stream = new AssertStream(fileSize, 42);
            cache.AddOrUpdateStream(123, stream, CancellationToken.None, null, false);

            Assert.IsTrue(stream.Closed);
            Assert.IsTrue(stream.Disposed);
        }

        [Test]
        public void StreamNotDisposed()
        {
            var cache = CreateCache();
            var fileSize = 10 * 1024 * 1024;

            var stream = new AssertStream(fileSize, 42);
            cache.AddOrUpdateStream(123, stream, CancellationToken.None, null, true);

            Assert.IsFalse(stream.Closed);
            Assert.IsFalse(stream.Disposed);
        }

        [Test]
        public void Performance()
        {
            var cache = CreateCache();
            var fileSize = 10 * 1024 * 1024;
            var iterCount = 10;

            Console.WriteLine("File size: {0:F1}mb", fileSize / (1024f * 1024f));
            Console.WriteLine("Iter count: {0}", iterCount);
            var sw = Stopwatch.StartNew();
            //heat up
            var ms = new MemoryStream();
            using (var cachedStream = cache.GetStreamOrAddStream(123, _ => GetRandomFile(fileSize),
                       CancellationToken.None, null))
            {
            }

            sw.Stop();
            var heatup = sw.Elapsed;
            Console.WriteLine("Heatup: {0}", sw.Elapsed);

            //test
            sw.Restart();
            for (var i = 0; i < iterCount; i++)
            {
                ms = new MemoryStream();
                using (var cachedStream = cache.GetStreamOrAddStream(123, _ => GetRandomFile(fileSize),
                           CancellationToken.None, null))
                {
                }
            }

            sw.Stop();
            var accessTime = new TimeSpan(sw.Elapsed.Ticks / iterCount);
            Console.WriteLine("Cache access time: {0}", accessTime);
            Console.WriteLine("Total access time: {0}", sw.Elapsed);

            Assert.Greater(heatup, accessTime);


            cache.GarbageCollect(CancellationToken.None);

            AssertNoGarbageFiles(cache);
        }

        private byte[] FullRead(IFileCache<long> cache, long key)
        {
            var ms = new MemoryStream();
            using (var cached = cache.TryGetStream(key, CancellationToken.None))
            {
                if (cached == null)
                    return null;
                cached.CopyTo(ms);
            }

            return ms.ToArray();
        }

        [Test]
        public void Invalidate()
        {
            var cache = CreateCache();
            var fileSize = 1 * 1024;

            var data = FullRead(cache, 123);
            Assert.IsNull(data);

            cache.AddOrUpdateStream(123, GetRandomFile(fileSize), CancellationToken.None, null);

            data = FullRead(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);

            cache.GarbageCollect(CancellationToken.None);

            data = FullRead(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);
            //
            cache.Invalidate(CancellationToken.None);
            //
            data = FullRead(cache, 123);
            Assert.IsNull(data);

            cache.AddOrUpdateStream(123, GetRandomFile(fileSize), CancellationToken.None, null);

            data = FullRead(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);

            cache.GarbageCollect(CancellationToken.None);

            data = FullRead(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);

            AssertNoGarbageFiles(cache);
        }

        [Test]
        public void InvalidateKey()
        {
            var cache = CreateCache();
            var fileSize = 1 * 1024;

            var data = FullRead(cache, 123);
            Assert.IsNull(data);

            cache.AddOrUpdateStream(123, GetRandomFile(fileSize), CancellationToken.None, null);

            data = FullRead(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);

            cache.GarbageCollect(CancellationToken.None);

            data = FullRead(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);
            //
            cache.Invalidate(123, CancellationToken.None);
            //
            data = FullRead(cache, 123);
            Assert.IsNull(data);

            cache.AddOrUpdateStream(123, GetRandomFile(fileSize), CancellationToken.None, null);

            data = FullRead(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);

            cache.GarbageCollect(CancellationToken.None);

            data = FullRead(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);

            AssertNoGarbageFiles(cache);
        }

        [Test]
        public void AbsoluteExpiration()
        {
            var cache = CreateCache();
            var fileSize = 1 * 1024;

            var data = FullRead(cache, 123);
            Assert.IsNull(data);
            //expired long time ago
            cache.AddOrUpdateStream(123, GetRandomFile(fileSize), CancellationToken.None,
                CacheExpirationPolicy.AbsoluteUtc(DateTime.MinValue));

            data = FullRead(cache, 123);
            Assert.IsNotNull(data);
            Assert.AreEqual(fileSize, data.Length);

            cache.GarbageCollect(CancellationToken.None); //here is expired one collected

            data = FullRead(cache, 123);
            Assert.IsNull(data);

            AssertNoGarbageFiles(cache);
        }

        [Test]
        [Category("FailOnNix")]
        public void LockedForReadFileGc()
        {
            var cache = CreateCache();
            var fileSize = 10;
            var data = FullRead(cache, 123);
            Assert.IsNull(data);
            cache.AddOrUpdateStream(123, GetRandomFile(fileSize), CancellationToken.None,
                CacheExpirationPolicy.AbsoluteUtc(DateTime.MinValue));
            data = FullRead(cache, 123);
            Assert.IsNotNull(data);

            var ms = new MemoryStream();
            using (var s = cache.TryGetStream(123, CancellationToken.None))
            {
                cache.GarbageCollect(CancellationToken.None); //item is expired but will not be collected.
                s.CopyTo(ms);
            }

            data = FullRead(cache, 123);
            Assert.IsNull(data);

            AssertHasCachedFiles(cache);

            cache.GarbageCollect(CancellationToken.None); //trash collected if not collected on read.

            AssertNoCachedFiles(cache);
            AssertNoGarbageFiles(cache);
        }

        [Test]
        public async Task AsyncLockedForReadFileGc()
        {
            try
            {
                var cache = CreateCache();
                var fileSize = 1 * 1024;
                var data = FullRead(cache, 123);
                Assert.IsNull(data);
                var tasks = Enumerable.Range(0, 20).Select(i =>
                {
                    try
                    {
                        var filePath = GetRandomPath();
                        cache.GetFileOrAddStream(123, x => GetRandomFile(fileSize), CancellationToken.None, filePath,
                            CacheExpirationPolicy.AbsoluteUtc(DateTime.MinValue));
                        cache.GarbageCollect(CancellationToken.None);
                        while (true)
                            try
                            {
                                AssertFile(filePath, fileSize);
                                break;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        throw;
                    }

                    return Task.CompletedTask;
                }).ToArray();

                await Task.WhenAll(tasks);

                cache.GarbageCollect(CancellationToken.None); //trash collected if not collected on read.

                AssertNoCachedFiles(cache);
                AssertNoGarbageFiles(cache);
            }
            finally
            {
                DeleteRandomPaths();
            }
        }

        [Test]
        public void SlidingExpiration()
        {
            var cache = CreateCache();
            var fileSize = 10;

            var slide = TimeSpan.FromMilliseconds(200);
            var calls = 10;

            var part = new TimeSpan(2 * slide.Ticks / calls);

            var data = FullRead(cache, 123);
            Assert.IsNull(data);
            //expired long time ago
            cache.AddOrUpdateStream(123, GetRandomFile(fileSize), CancellationToken.None,
                CacheExpirationPolicy.SlidingUtc(slide));

            for (var i = 0; i < calls; i++)
            {
                data = FullRead(cache, 123);
                Assert.IsNotNull(data);
                Assert.AreEqual(fileSize, data.Length);
                cache.GarbageCollect(CancellationToken.None); //here is expired one collected
                Thread.Sleep(part);
            }

            Thread.Sleep(new TimeSpan(2 * slide.Ticks));
            cache.GarbageCollect(CancellationToken.None); //here is expired one collected

            data = FullRead(cache, 123);
            Assert.IsNull(data);

            AssertNoGarbageFiles(cache);
        }

        [Test]
        public void CancellationSpam()
        {
            var cache = CreateCache();
            var fileSize = 1L * 1024 * 1024 * 1024 * 1024; //1 TB

            var calls = 100;


            for (var i = 0; i < calls; i++)
                using (var cts = new CancellationTokenSource())
                {
                    cts.CancelAfter(10);
                    try
                    {
                        cache.AddOrUpdateStream(123, GetRandomFile(fileSize), cts.Token, null);
                    }
                    catch (OperationCanceledException)
                    {
                        //good.
                    }
                }

            cache.GarbageCollect(CancellationToken.None);

            AssertNoGarbageFiles(cache);
        }

        [Test]
        public void PerformanceCopyOfFile()
        {
            try
            {
                var cache = CreateCache();
                var fileSize = 10L * 1024 * 1024;
                var iterCount = 10;

                Console.WriteLine("File size: {0:F1}mb", fileSize / (1024f * 1024f));
                Console.WriteLine("Iter count: {0}", iterCount);
                var sw = Stopwatch.StartNew();
                //heat up
                var targetFilePath = GetRandomPath();
                cache.GetFileOrAddStream(123, _ => GetRandomFile(fileSize), CancellationToken.None,
                    targetFilePath, null);
                sw.Stop();
                AssertFile(targetFilePath, fileSize);


                var heatup = sw.Elapsed;
                Console.WriteLine("Heatup: {0}", sw.Elapsed);


                //test
                sw.Restart();
                for (var i = 0; i < iterCount; i++)
                {
                    targetFilePath = GetRandomPath();
                    cache.GetFileOrAddStream(123, _ => GetRandomFile(fileSize), CancellationToken.None,
                        targetFilePath, null);
                }

                sw.Stop();
                var accessTime = new TimeSpan(sw.Elapsed.Ticks / iterCount);
                Console.WriteLine("Cache access time: {0}", accessTime);
                Console.WriteLine("Total access time: {0}", sw.Elapsed);

                Assert.Greater(heatup, accessTime);


                cache.GarbageCollect(CancellationToken.None);

                AssertNoGarbageFiles(cache);
            }
            finally
            {
                DeleteRandomPaths();
            }
        }

        private void AssertFile(string filePath, long fileSize)
        {
            Assert.IsTrue(File.Exists(filePath));
            Assert.AreEqual(fileSize, new FileInfo(filePath).Length);
            using (var s = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                s.ReadByte();
            }
        }

        [Test]
        public void LockedForReadHardlinkGc()
        {
            try
            {
                var cache = CreateCache();
                var fileSize = 1 * 1024 * 1024; // 1 mb

                cache.AddOrUpdateStream(123, GetRandomFile(fileSize), CancellationToken.None,
                    CacheExpirationPolicy.AbsoluteUtc(DateTime.MinValue));
                var data = FullRead(cache, 123);
                Assert.IsNotNull(data);


                var targetFilePath = GetRandomPath();
                Assert.IsTrue(cache.TryGetFile(123, CancellationToken.None, targetFilePath));
                try
                {
                    AssertFile(targetFilePath, fileSize);
                    AssertHasCachedFiles(cache);

                    using (var s = File.OpenRead(targetFilePath))
                    {
                        var bytes = new byte[8 * 1024];
                        Assert.IsTrue(s.Read(bytes, 0, bytes.Length) > 0);
                        cache.GarbageCollect(CancellationToken.None);
                    }

                    AssertFile(targetFilePath, fileSize); //item is expired, collected, but available to user

                    data = FullRead(cache, 123);
                    Assert.IsNull(data);

                    cache.GarbageCollect(CancellationToken.None); //trash collected if not collected on read.

                    AssertNoCachedFiles(cache);
                    AssertNoGarbageFiles(cache);

                    AssertFile(targetFilePath, fileSize);
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