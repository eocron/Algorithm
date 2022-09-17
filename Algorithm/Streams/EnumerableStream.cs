using System;
using System.Collections.Generic;
using System.IO;

namespace Eocron.Algorithms.Streams
{
    public sealed class EnumerableStream : Stream
    {
        private readonly Lazy<IEnumerator<Memory<byte>>> _enumerator;
        private Memory<byte>? _currentReadableBuffer;
        private bool _eos;

        public EnumerableStream(IEnumerable<Memory<byte>> enumerable)
        {
            _enumerator = new Lazy<IEnumerator<Memory<byte>>>(enumerable.GetEnumerator);
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Read(new Span<byte>(buffer, offset, count));
        }

        public override int Read(Span<byte> dst)
        {
            if (_eos)
                return 0;

            int read = 0;

            while (true)
            {
                if (_currentReadableBuffer != null)
                {
                    var src = _currentReadableBuffer.Value;
                    if (src.Length > dst.Length)
                    {
                        src.Slice(0, dst.Length).Span.CopyTo(dst);
                        read += dst.Length;
                        _currentReadableBuffer = src.Slice(dst.Length, src.Length - dst.Length);
                        break;
                    }
                    else if (src.Length < dst.Length)
                    {
                        src.Span.CopyTo(dst);
                        dst = dst.Slice(src.Length, dst.Length - src.Length);
                        read += src.Length;
                        _currentReadableBuffer = null;
                        //need more transformations to fill destination
                    }
                    else
                    {
                        src.Span.CopyTo(dst);
                        read += src.Length;
                        _currentReadableBuffer = null;
                        break;
                    }
                }
                else
                {
                    if (!_enumerator.Value.MoveNext())
                    {
                        _eos = true;
                        _enumerator.Value.Dispose();
                        break;
                    }
                    else
                    {
                        _currentReadableBuffer = _enumerator.Value.Current;
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

        public override void Close()
        {
            if (_enumerator.IsValueCreated)
            {
                _enumerator.Value.Dispose();
            }
            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (_enumerator.IsValueCreated)
            {
                _enumerator.Value.Dispose();
            }
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