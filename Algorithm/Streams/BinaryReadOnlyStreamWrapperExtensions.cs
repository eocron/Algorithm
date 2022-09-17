using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.Streams
{
    public static class BinaryReadOnlyStreamWrapperExtensions
    {
        private static Memory<byte> DefaultBufferProvider()
        {
            return new Memory<byte>(new byte[8 * 1024]);
        }

        public static IEnumerable<Memory<byte>> AsEnumerable(this Stream stream)
        {
            return new BinaryReadOnlyStreamWrapper(() => stream, DefaultBufferProvider);
        }

        public static IAsyncEnumerable<Memory<byte>> AsAsyncEnumerable(this Stream stream)
        {
            return new BinaryReadOnlyStreamWrapper(() => stream, DefaultBufferProvider);
        }

        public static byte[] ToByteArray(this IEnumerable<Memory<byte>> stream)
        {
            using var ms = new MemoryStream();
            foreach (var memory in stream)
            {
                ms.Write(memory.Span);
            }

            return ms.ToArray();
        }

        public static async Task<byte[]> ToByteArrayAsync(this IAsyncEnumerable<Memory<byte>> stream,
            CancellationToken ct = default)
        {
            using var ms = new MemoryStream();
            await foreach (var memory in stream.WithCancellation(ct).ConfigureAwait(false))
            {
                ms.Write(memory.Span);
            }

            return ms.ToArray();
        }

        public static IEnumerable<Memory<byte>> GZip(this IEnumerable<Memory<byte>> stream, CompressionMode mode)
        {
            return new BinaryReadOnlyStreamWrapper(
                () => mode == CompressionMode.Decompress
                    ? (Stream)new GZipStream(new EnumerableStream(stream), mode, false)
                    : (Stream)new WriteToReadStream<GZipStream>(
                        x => new GZipStream(x, mode, false),
                        () => new EnumerableStream(stream),
                        (x, ct) => x.FlushAsync(ct),
                        x => x.Flush()),
                DefaultBufferProvider);
        }

        public static IAsyncEnumerable<Memory<byte>> GZip(this IAsyncEnumerable<Memory<byte>> stream, CompressionMode mode)
        {
            return new BinaryReadOnlyStreamWrapper(
                () => mode == CompressionMode.Decompress
                    ? (Stream)new GZipStream(new AsyncEnumerableStream(stream), mode, false)
                    : (Stream)new WriteToReadStream<GZipStream>(
                        x => new GZipStream(x, mode, false),
                        () => new AsyncEnumerableStream(stream),
                        (x, ct) => x.FlushAsync(ct),
                        x => x.Flush()),
                DefaultBufferProvider);
        }

        public static IEnumerable<Memory<byte>> CryptoTransform(this IEnumerable<Memory<byte>> stream,
            ICryptoTransform transform, CryptoStreamMode mode)
        {
            return new BinaryReadOnlyStreamWrapper(
                () => mode == CryptoStreamMode.Read
                    ? (Stream)new CryptoStream(new EnumerableStream(stream), transform, mode, false)
                    : (Stream)new WriteToReadStream<CryptoStream>(
                        x => new CryptoStream(x, transform, mode, false),
                        () => new EnumerableStream(stream),
                        async (x, ct) => x.FlushFinalBlock(),
                        x => x.FlushFinalBlock()),
                DefaultBufferProvider);
        }

        public static IAsyncEnumerable<Memory<byte>> CryptoTransform(this IAsyncEnumerable<Memory<byte>> stream,
            ICryptoTransform transform, CryptoStreamMode mode)
        {
            return new BinaryReadOnlyStreamWrapper(
                () => mode == CryptoStreamMode.Read
                    ? (Stream)new CryptoStream(new AsyncEnumerableStream(stream), transform, mode, false)
                    : (Stream)new WriteToReadStream<CryptoStream>(
                        x => new CryptoStream(x, transform, mode, false),
                        () => new AsyncEnumerableStream(stream),
                        async (x, ct) => x.FlushFinalBlock(),
                        x => x.FlushFinalBlock()),
                DefaultBufferProvider);
        }
    }
}