using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.Streams
{
    public static class StringStreamExtensions
    {
        public static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IEnumerable<T> enumerable)
        {
            if(enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));
            foreach (var e in enumerable)
            {
                yield return e;
            }
        }

        public static IEnumerable<Memory<char>> Convert(this IEnumerable<Memory<byte>> enumerable, Encoding encoding)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));
            var buffer = BinaryStreamExtensions.DefaultBufferProvider<char>();
            return enumerable.Select(x =>
            {
                var read = encoding.GetChars(x.Span, buffer.Span);
                return buffer.Slice(0, read);
            });
        }

        public static async IAsyncEnumerable<Memory<char>> Convert(this IAsyncEnumerable<Memory<byte>> enumerable, Encoding encoding)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));
            var buffer = BinaryStreamExtensions.DefaultBufferProvider<char>();
            await foreach (var e in enumerable.ConfigureAwait(false))
            {
                var read = encoding.GetChars(e.Span, buffer.Span);
                yield return buffer.Slice(0, read);
            }
        }

        public static IEnumerable<Memory<byte>> Convert(this IEnumerable<Memory<char>> enumerable, Encoding encoding)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));
            var buffer = BinaryStreamExtensions.DefaultBufferProvider<byte>();
            return enumerable.Select(x =>
            {
                var read = encoding.GetBytes(x.Span, buffer.Span);
                return buffer.Slice(0, read);
            });
        }

        public static async IAsyncEnumerable<Memory<byte>> Convert(this IAsyncEnumerable<Memory<char>> enumerable, Encoding encoding)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));
            var buffer = BinaryStreamExtensions.DefaultBufferProvider<byte>();
            await foreach (var e in enumerable.ConfigureAwait(false))
            {
                var read = encoding.GetBytes(e.Span, buffer.Span);
                yield return buffer.Slice(0, read);
            }
        }

        public static string BuildString(this IEnumerable<Memory<char>> enumerable)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));
            var sb = new StringBuilder();
            foreach (var memory in enumerable)
            {
                sb.Append(memory.Span);
            }
            return sb.ToString();
        }

        public static async Task<string> BuildStringAsync(this IAsyncEnumerable<Memory<char>> enumerable, CancellationToken cancellationToken)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));
            var sb = new StringBuilder();
            await foreach (var memory in enumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                sb.Append(memory.Span);
            }
            return sb.ToString();
        }
    }
}