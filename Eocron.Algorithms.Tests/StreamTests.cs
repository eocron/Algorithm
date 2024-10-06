using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Eocron.Algorithms.Randoms;
using Eocron.Algorithms.Streams;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class StreamTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            var seed = (int)DateTime.UtcNow.Ticks;
            var rnd = new Random(seed);
            Console.WriteLine($"seed: {seed}");

            TestData = new byte[rnd.Next(1000, 100000)];
            rnd.NextBytes(TestData);

            TestDataString = rnd.NextString(rnd.Next(100, 500));
        }

        private byte[] TestData { get; set; }
        private string TestDataString { get; set; }

        [Test]
        public void Catch()
        {
            var testStream = GetTestStream();
            ClassicAssert.Throws<InvalidDataException>(() =>
            {
                var actual = testStream
                    .AsEnumerable()
                    .GZip(CompressionMode.Decompress)
                    .ToByteArray();
            });

            ClassicAssert.AreEqual(1, testStream.CloseCallCount);
            ClassicAssert.AreEqual(0, testStream.DisposeAsyncCallCount);
            ClassicAssert.AreEqual(1, testStream.DisposeCallCount);
        }

        [Test]
        public void CatchAsync()
        {
            var testStream = GetTestStream();
            ClassicAssert.ThrowsAsync<InvalidDataException>(async () =>
            {
                var actual = await testStream
                    .AsAsyncEnumerable()
                    .GZip(CompressionMode.Decompress)
                    .ToByteArrayAsync();
            });

            ClassicAssert.AreEqual(1, testStream.CloseCallCount);
            ClassicAssert.AreEqual(1, testStream.DisposeAsyncCallCount);
            ClassicAssert.AreEqual(1, testStream.DisposeCallCount);
        }

        [Test]
        public void Read()
        {
            var testStream = new TestReadStream(new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }));
            var result = testStream
                .AsEnumerable(new TestMemoryPool<byte>(2))
                .Select(x => x.ToArray())
                .ToList();
            ClassicAssert.AreEqual(3, result.Count);
            CollectionAssert.AreEqual(new[] { 1, 2 }, result[0]);
            CollectionAssert.AreEqual(new[] { 3, 4 }, result[1]);
            CollectionAssert.AreEqual(new[] { 5 }, result[2]);

            ClassicAssert.AreEqual(1, testStream.CloseCallCount);
            ClassicAssert.AreEqual(0, testStream.DisposeAsyncCallCount);
            ClassicAssert.AreEqual(1, testStream.DisposeCallCount);
        }

        [Test]
        public void ReadNonDisposable()
        {
            using var testStream = new TestReadStream(new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }));
            var result = testStream
                .AsEnumerable(new TestMemoryPool<byte>(2), true)
                .Select(x => x.ToArray())
                .ToList();
            ClassicAssert.AreEqual(3, result.Count);
            CollectionAssert.AreEqual(new[] { 1, 2 }, result[0]);
            CollectionAssert.AreEqual(new[] { 3, 4 }, result[1]);
            CollectionAssert.AreEqual(new[] { 5 }, result[2]);

            ClassicAssert.AreEqual(0, testStream.CloseCallCount);
            ClassicAssert.AreEqual(0, testStream.DisposeAsyncCallCount);
            ClassicAssert.AreEqual(0, testStream.DisposeCallCount);
        }

        [Test]
        public void GZip()
        {
            var testStream = GetTestStream();
            var actual = testStream
                .AsEnumerable()
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .ToByteArray();
            CollectionAssert.AreEqual(TestData, actual);
            ClassicAssert.AreEqual(1, testStream.CloseCallCount);
            ClassicAssert.AreEqual(0, testStream.DisposeAsyncCallCount);
            ClassicAssert.AreEqual(1, testStream.DisposeCallCount);
        }

        [Test]
        public void GZipNonDisposable()
        {
            using var testStream = GetTestStream();
            var actual = testStream
                .AsEnumerable(leaveOpen: true)
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .ToByteArray();
            CollectionAssert.AreEqual(TestData, actual);
            ClassicAssert.AreEqual(0, testStream.CloseCallCount);
            ClassicAssert.AreEqual(0, testStream.DisposeAsyncCallCount);
            ClassicAssert.AreEqual(0, testStream.DisposeCallCount);
        }

        [Test]
        public async Task GZipAsync()
        {
            var testStream = GetTestStream();
            var actual = await testStream
                .AsAsyncEnumerable()
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .ToByteArrayAsync(CancellationToken.None);
            CollectionAssert.AreEqual(TestData, actual);
            ClassicAssert.AreEqual(1, testStream.CloseCallCount);
            ClassicAssert.AreEqual(1, testStream.DisposeAsyncCallCount);
            ClassicAssert.AreEqual(1, testStream.DisposeCallCount);
        }

        [Test]
        public async Task GZipNonDisposableAsync()
        {
            await using var testStream = GetTestStream();
            var actual = await testStream
                .AsAsyncEnumerable(leaveOpen: true)
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .ToByteArrayAsync(CancellationToken.None);
            CollectionAssert.AreEqual(TestData, actual);
            ClassicAssert.AreEqual(0, testStream.CloseCallCount);
            ClassicAssert.AreEqual(0, testStream.DisposeAsyncCallCount);
            ClassicAssert.AreEqual(0, testStream.DisposeCallCount);
        }

        [Test]
        public void String()
        {
            var actual = new[] { new Memory<char>(TestDataString.ToCharArray()) }
                .AsEnumerable()
                .Convert(Encoding.Default)
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .Convert(Encoding.Default)
                .BuildString();

            ClassicAssert.AreEqual(TestDataString, actual);
        }

        [Test]
        public async Task StringAsync()
        {
            var actual = await new[] { new Memory<char>(TestDataString.ToCharArray()) }
                .AsAsyncEnumerable()
                .Convert(Encoding.Default)
                .GZip(CompressionMode.Compress)
                .GZip(CompressionMode.Decompress)
                .Convert(Encoding.Default)
                .BuildStringAsync(CancellationToken.None);

            ClassicAssert.AreEqual(TestDataString, actual);
        }

        private TestReadStream GetTestStream()
        {
            return new TestReadStream(new MemoryStream(TestData));
        }

        private class TestMemoryPool<T> : MemoryPool<T>
        {
            public TestMemoryPool(int bufferSize)
            {
                _bufferSize = bufferSize;
            }

            public override IMemoryOwner<T> Rent(int minBufferSize = -1)
            {
                return new TestMemoryOwner(new Memory<T>(new T[_bufferSize]));
            }

            protected override void Dispose(bool disposing)
            {
            }

            public override int MaxBufferSize => _bufferSize;
            private readonly int _bufferSize;

            private class TestMemoryOwner : IMemoryOwner<T>
            {
                public TestMemoryOwner(Memory<T> inner)
                {
                    Memory = inner;
                }

                public void Dispose()
                {
                }

                public Memory<T> Memory { get; }
            }
        }

        private class TestReadStream : Stream
        {
            public TestReadStream(Stream streamImplementation)
            {
                _streamImplementation = streamImplementation;
            }

            public override void Close()
            {
                CloseCallCount++;
                _streamImplementation.Close();
                base.Close();
            }

            public override async ValueTask DisposeAsync()
            {
                DisposeAsyncCallCount++;
                await _streamImplementation.DisposeAsync();
                await base.DisposeAsync();
            }

            public override void Flush()
            {
                ClassicAssert.Fail(nameof(Flush));
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _streamImplementation.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                ClassicAssert.Fail(nameof(Seek));
                return -1;
            }

            public override void SetLength(long value)
            {
                ClassicAssert.Fail(nameof(SetLength));
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                ClassicAssert.Fail(nameof(Write));
            }

            protected override void Dispose(bool disposing)
            {
                DisposeCallCount++;
                _streamImplementation.Dispose();
                base.Dispose(disposing);
            }

            public override bool CanRead => _streamImplementation.CanRead;

            public override bool CanSeek => _streamImplementation.CanSeek;

            public override bool CanWrite => _streamImplementation.CanWrite;

            public override long Length => _streamImplementation.Length;

            public override long Position
            {
                get => _streamImplementation.Position;
                set => ClassicAssert.Fail(nameof(Position));
            }

            private readonly Stream _streamImplementation;

            public int CloseCallCount;

            public int DisposeAsyncCallCount;
            public int DisposeCallCount;
        }
    }
}