using System;
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
        private readonly Lazy<Stream> _dataSource;
        private readonly MemoryStream _readable;
        private Memory<byte>? _currentReadableBuffer;
        private readonly Lazy<T> _writable;
        private byte[] _transferBuffer;
        private bool _eos;

        public WriteToReadStream(Func<Stream, T> writableStreamProvider,
            Func<Stream> dataSourceProvider,
            Func<T, CancellationToken, Task> onEosAsync,
            Action<T> onEos)
        {
            _onEosAsync = onEosAsync;
            _onEos = onEos;
            _dataSource = new Lazy<Stream>(dataSourceProvider);
            _readable = new MemoryStream();
            _writable = new Lazy<T>(() => writableStreamProvider(_readable));
            _currentReadableBuffer = null;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var dst = new Memory<byte>(buffer, offset, count);
            EnsureTransferBuffer(dst.Length);
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
                    if (IsEos())
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
                    if (IsEos())
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
            Debug.Assert(_currentReadableBuffer == null, "Readable buffer should be emptied out before transform");
            Debug.Assert(!_eos, "Eos reached but transform was called");

            var read = _dataSource.Value.Read(_transferBuffer);
            if (read <= 0)
                _eos = true;
            else
                _writable.Value.Write(_transferBuffer, 0, read);

            if (_eos && _writable.IsValueCreated)
            {
                _onEos(_writable.Value);
            }

            if (_readable.Position > 0)
            {
                _currentReadableBuffer = _readable.GetBuffer().AsMemory(0, (int)_readable.Position);
                _readable.Seek(0, SeekOrigin.Begin);
            }
        }

        private async Task TransformAsync(CancellationToken ct)
        {
            Debug.Assert(_currentReadableBuffer == null, "Readable buffer should be emptied out before transform");
            Debug.Assert(!_eos, "Eos reached but transform was called");

            var read = await _dataSource.Value.ReadAsync(_transferBuffer, ct).ConfigureAwait(false);
            if (read <= 0)
                _eos = true;
            else
                await _writable.Value.WriteAsync(_transferBuffer, 0, read, ct).ConfigureAwait(false);

            if (_eos && _writable.IsValueCreated)
            {
                await _onEosAsync(_writable.Value, ct).ConfigureAwait(false);
            }

            if (_readable.Position > 0)
            {
                _currentReadableBuffer = _readable.GetBuffer().AsMemory(0, (int)_readable.Position);
                _readable.Seek(0, SeekOrigin.Begin);
            }
        }

        private void EnsureTransferBuffer(int desiredBufferSize)
        {
            if (_transferBuffer == null)
            {
                _transferBuffer = new byte[desiredBufferSize];
            }
        }

        private bool IsEos()
        {
            return _eos;
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
            if (_writable.IsValueCreated)
                _writable.Value.Dispose();
            if(_dataSource.IsValueCreated)
                _dataSource.Value.Dispose();
            _readable.Dispose();
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