using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Eocron.Algorithms.Streams;
using NUnit.Framework;

namespace NTests
{
    [TestFixture]
    public class StreamTests
    {
        private byte[] TestData { get; set; }

        [OneTimeSetUp]
        public void Setup()
        {
            var seed = (int)DateTime.UtcNow.Ticks;
            var rnd = new Random(seed);
            Console.WriteLine($"seed: {seed}");

            TestData = new byte[rnd.Next(1000, 100000)];
            rnd.NextBytes(TestData);
        }

        [Test]
        public void Read()
        {
            var result = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 })
                .AsEnumerable(() => new Memory<byte>(new byte[2]))
                .Select(x => x.ToArray())
                .ToList();
            Assert.AreEqual(3, result.Count);
            CollectionAssert.AreEqual(new[] { 1, 2 }, result[0]);
            CollectionAssert.AreEqual(new[] { 3, 4 }, result[1]);
            CollectionAssert.AreEqual(new[] { 5 }, result[2]);
        }


        [Test]
        public void GZip()
        {
            var actual = new MemoryStream(TestData)
                .AsEnumerable()
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .ToByteArray();
            CollectionAssert.AreEqual(TestData, actual);
        }

        [Test]
        public async Task GZipAsync()
        {
            var actual = await new MemoryStream(TestData)
                .AsAsyncEnumerable()
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .ToByteArrayAsync(CancellationToken.None);
            CollectionAssert.AreEqual(TestData, actual);
        }
    }
}
