using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.Streams
{
    public sealed class WriteToReadStream<T> : Stream
        where T : Stream
    {
        private readonly Func<T, CancellationToken, Task> _onEosAsync;
        private readonly Action<T> _onEos;
        private readonly MemoryPool<byte> _pool;
        private readonly Lazy<Stream> _sourceStream;
        private readonly MemoryStream _transferStream;
        private readonly Lazy<T> _targetStream;

        private IMemoryOwner<byte> _transferBuffer;
        private Memory<byte>? _transferBufferLeftover;
        private bool _eos;

        public WriteToReadStream(Func<Stream> sourceStreamProvider,
            Func<Stream, T> targetStreamProvider,
            MemoryPool<byte> pool = null,
            Func<T, CancellationToken, Task> onEosAsync = null,
            Action<T> onEos = null)
        {
            _sourceStream = new Lazy<Stream>(sourceStreamProvider);
            _transferStream = new MemoryStream();
            _targetStream = new Lazy<T>(() => targetStreamProvider(_transferStream));
            _transferBufferLeftover = null;
            _onEosAsync = onEosAsync ?? ((x, ct) => x.FlushAsync());
            _onEos = onEos ?? (x => x.Flush());
            _pool = pool ?? BufferingConstants<byte>.DefaultMemoryPool;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var dst = new Memory<byte>(buffer, offset, count);
            EnsureTransferBuffer(dst.Length);
            int read = 0;

            while (true)
            {
                if (_transferBufferLeftover != null)
                {
                    var src = _transferBufferLeftover.Value;
                    if (src.Length > dst.Length)
                    {
                        src.Slice(0, dst.Length).CopyTo(dst);
                        read += dst.Length;
                        _transferBufferLeftover = src.Slice(dst.Length, src.Length - dst.Length);
                        break;
                    }
                    else if (src.Length < dst.Length)
                    {
                        src.CopyTo(dst);
                        dst = dst.Slice(src.Length, dst.Length - src.Length);
                        read += src.Length;
                        _transferBufferLeftover = null;
                        //need more transformations to fill destination
                    }
                    else
                    {
                        src.CopyTo(dst);
                        read += src.Length;
                        _transferBufferLeftover = null;
                        break;
                    }
                }
                else
                {
                    if (_eos)
                    {
                        break;
                    }
                    else
                    {
                        Transform();
                    }
                }
            }

            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            var dst = new Memory<byte>(buffer, offset, count);
            EnsureTransferBuffer(dst.Length);
            int read = 0;

            while (true)
            {
                if (_transferBufferLeftover != null)
                {
                    var src = _transferBufferLeftover.Value;
                    if (src.Length > dst.Length)
                    {
                        src.Slice(0, dst.Length).CopyTo(dst);
                        read += dst.Length;
                        _transferBufferLeftover = src.Slice(dst.Length, src.Length - dst.Length);
                        break;
                    }
                    else if (src.Length < dst.Length)
                    {
                        src.CopyTo(dst);
                        dst = dst.Slice(src.Length, dst.Length - src.Length);
                        read += src.Length;
                        _transferBufferLeftover = null;
                        //need more transformations to fill destination
                    }
                    else
                    {
                        src.CopyTo(dst);
                        read += src.Length;
                        _transferBufferLeftover = null;
                        break;
                    }
                }
                else
                {
                    if (_eos)
                    {
                        break;
                    }
                    else
                    {
                        await TransformAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return read;
        }

        private void Transform()
        {
            Debug.Assert(_transferBufferLeftover == null, "Readable buffer should be emptied out before transform");
            Debug.Assert(!_eos, "Eos reached but transform was called");

            var read = _sourceStream.Value.Read(_transferBuffer.Memory.Span);
            if (read <= 0)
                _eos = true;
            else
                _targetStream.Value.Write(_transferBuffer.Memory.Slice(0, read).Span);

            if (_eos && _targetStream.IsValueCreated)
            {
                _onEos(_targetStream.Value);
            }

            if (_transferStream.Position > 0)
            {
                _transferBufferLeftover = _transferStream.GetBuffer().AsMemory(0, (int)_transferStream.Position);
                _transferStream.Seek(0, SeekOrigin.Begin);
            }
        }

        private async Task TransformAsync(CancellationToken ct)
        {
            Debug.Assert(_transferBufferLeftover == null, "Readable buffer should be emptied out before transform");
            Debug.Assert(!_eos, "Eos reached but transform was called");

            var read = await _sourceStream.Value.ReadAsync(_transferBuffer.Memory, ct).ConfigureAwait(false);
            if (read <= 0)
                _eos = true;
            else
                await _targetStream.Value.WriteAsync(_transferBuffer.Memory.Slice(0, read), ct).ConfigureAwait(false);

            if (_eos && _targetStream.IsValueCreated)
            {
                await _onEosAsync(_targetStream.Value, ct).ConfigureAwait(false);
            }

            if (_transferStream.Position > 0)
            {
                _transferBufferLeftover = _transferStream.GetBuffer().AsMemory(0, (int)_transferStream.Position);
                _transferStream.Seek(0, SeekOrigin.Begin);
            }
        }

        private void EnsureTransferBuffer(int desiredBufferSize)
        {
            if (desiredBufferSize <= BufferingConstants<byte>.DefaultBufferSize)
            {
                desiredBufferSize = BufferingConstants<byte>.DefaultBufferSize;
            }
            if (_transferBuffer == null)
            {
                _transferBuffer = _pool.Rent(desiredBufferSize);
            }
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
            if (_targetStream.IsValueCreated)
                _targetStream.Value.Dispose();
            if(_sourceStream.IsValueCreated)
                _sourceStream.Value.Dispose();
            _transferStream.Dispose();
            _transferBuffer.Dispose();
            base.Dispose(disposing);
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