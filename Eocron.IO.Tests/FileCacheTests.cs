using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Eocron.IO.Caching;
using Eocron.IO.Files;
using FluentAssertions;
using NUnit.Framework;

namespace Eocron.IO.Tests
{
    [TestFixture]
    public class FileCacheTests
    {
        [Test]
        public async Task GetOrAddFile()
        {
            await using (var result =
                         await _cache.GetOrAddFileAsync("key1", _testFileName, GetFilePathProvider, retainSource: true))
            {
                await AssertFileEqual(result, _testFilePath, _testFileName);
            }

            (await _cache.ContainsKeyAsync("key1")).Should().BeTrue();
            (await _cache.ContainsKeyAsync("key2")).Should().BeFalse();
            (await _cache.TryRemoveAsync("key1")).Should().BeTrue();
            (await _cache.TryRemoveAsync("key1")).Should().BeFalse();
            (await _cache.TryRemoveAsync("key2")).Should().BeFalse();
            (await _cache.ContainsKeyAsync("key1")).Should().BeFalse();
            (await _cache.ContainsKeyAsync("key2")).Should().BeFalse();
        }

        private async Task AssertFileEqual(IFileCacheLink actualLink, string expectedFilePath, string expectedFileName)
        {
            var expected = await File.ReadAllBytesAsync(expectedFilePath);
            var actual = ReadToEnd(actualLink.OpenRead());
            var actualFilePath = actualLink.UnsafeFilePath;
            var actualFileName = Path.GetFileName(actualFilePath);
            var actualFileContent = await File.ReadAllBytesAsync(actualFilePath);


            actualFileName.Should().Be(expectedFileName);
            actualFileContent.Should().BeEquivalentTo(expected);
            actual.Should().BeEquivalentTo(expected);
        }

        private static byte[] ReadToEnd(Stream stream)
        {
            using var _ = stream;
            using var ms = new MemoryStream();
            stream.CopyTo(ms);

            return ms.ToArray();
        }
        
        public async Task<Stream> GetFileStreamProvider(string key, CancellationToken ct)
        {
            return File.OpenRead(_testFilePath);
        }
        public async Task<string> GetFilePathProvider(string key, CancellationToken ct)
        {
            return _testFilePath;
        }

        [OneTimeSetUp]
        public void Setup()
        {
            _cachePath = Path.Combine(Path.GetTempPath(), nameof(FileCacheTests));
            _testFilePath = Path.GetTempFileName();
            File.WriteAllText(_testFilePath, Guid.NewGuid().ToString());
            _testFileName = "test.txt";
            _fs = new FileSystem(_cachePath);
            _lp = new InMemoryFileCacheLockProvider();
            _cache = new FileCache(_fs, MD5.Create(), _lp);
        }
        [OneTimeTearDown]
        public void TearDown()
        {
            File.Delete(_testFilePath);
            Directory.Delete(_cachePath, true);
        }
        
        private string _testFilePath;
        private FileSystem _fs;
        private IFileCache _cache;
        private InMemoryFileCacheLockProvider _lp;
        private string _testFileName;
        private string _cachePath;
    }
}