using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.Streams
{
    public sealed class EnumerableStream : Stream
    {
        private readonly IAsyncEnumerable<Memory<byte>> _asyncEnumerable;
        private IAsyncEnumerator<Memory<byte>> _asyncEnumerator;

        private readonly IEnumerable<Memory<byte>> _enumerable;
        private IEnumerator<Memory<byte>> _enumerator;

        private bool IsAsync => _asyncEnumerable != null;

        private Memory<byte>? _currentReadableBuffer;
        private bool _eos;

        public EnumerableStream(IEnumerable<Memory<byte>> enumerable)
        {
            _enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));
        }

        public EnumerableStream(IAsyncEnumerable<Memory<byte>> enumerable)
        {
            _asyncEnumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_eos)
                return 0;

            if (!IsAsync)
                return Read(buffer, offset, count);

            _asyncEnumerator ??= _asyncEnumerable.GetAsyncEnumerator(cancellationToken);

            int read = 0;
            var dst = new Memory<byte>(buffer, offset, count);
            while (true)
            {
                if (_currentReadableBuffer != null)
                {
                    var src = _currentReadableBuffer.Value;
                    if (src.Length > dst.Length)
                    {
                        src.Slice(0, dst.Length).CopyTo(dst);
                        read += dst.Length;
                        _currentReadableBuffer = src.Slice(dst.Length, src.Length - dst.Length);
                        break;
                    }
                    else if (src.Length < dst.Length)
                    {
                        src.CopyTo(dst);
                        dst = dst.Slice(src.Length, dst.Length - src.Length);
                        read += src.Length;
                        _currentReadableBuffer = null;
                        //need more transformations to fill destination
                    }
                    else
                    {
                        src.CopyTo(dst);
                        read += src.Length;
                        _currentReadableBuffer = null;
                        break;
                    }
                }
                else
                {
                    if (!await _asyncEnumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        _eos = true;
                        break;
                    }
                    else
                    {
                        _currentReadableBuffer = _asyncEnumerator.Current;
                    }
                }
            }

            return read;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_eos)
                return 0;

            if (IsAsync)
                return ReadAsync(buffer, offset, count, CancellationToken.None).Result;

            _enumerator ??= _enumerable.GetEnumerator();
            var dst = new Memory<byte>(buffer, offset, count);
            int read = 0;

            while (true)
            {
                if (_currentReadableBuffer != null)
                {
                    var src = _currentReadableBuffer.Value;
                    if (src.Length > dst.Length)
                    {
                        src.Slice(0, dst.Length).CopyTo(dst);
                        read += dst.Length;
                        _currentReadableBuffer = src.Slice(dst.Length, src.Length - dst.Length);
                        break;
                    }
                    else if (src.Length < dst.Length)
                    {
                        src.CopyTo(dst);
                        dst = dst.Slice(src.Length, dst.Length - src.Length);
                        read += src.Length;
                        _currentReadableBuffer = null;
                        //need more transformations to fill destination
                    }
                    else
                    {
                        src.CopyTo(dst);
                        read += src.Length;
                        _currentReadableBuffer = null;
                        break;
                    }
                }
                else
                {
                    if (!_enumerator.MoveNext())
                    {
                        _eos = true;
                        break;
                    }
                    else
                    {
                        _currentReadableBuffer = _enumerator.Current;
                    }
                }
            }

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            _asyncEnumerator?.DisposeAsync().AsTask().Wait();
            _enumerator?.Dispose();
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (_asyncEnumerator != null)
            {
                await _asyncEnumerator.DisposeAsync().ConfigureAwait(false);
            }
            _enumerator?.Dispose();
            await base.DisposeAsync().ConfigureAwait(false);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
    }
}