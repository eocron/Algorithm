using System;
using System.IO;

namespace Algorithm.Disposing
{
    /// <summary>
    /// Safe way to wrap nesting streams into one for return.
    /// For example you can open file stream, wrap it into crypto stream, then return this aggregated stream, which will use
    /// crypto stream interface and afterwards will dispose file stream along with crypto stream.
    /// </summary>
    public class DisposableStream : Stream
    {
        public override bool CanRead => _inner.CanRead;

        public override bool CanSeek => _inner.CanSeek;

        public override bool CanWrite => _inner.CanWrite;

        public override long Length => _inner.Length;

        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        private readonly IDisposable _disposable;
        private readonly Stream _inner;

        public DisposableStream(Stream inner, IDisposable disposable)
        {
            if (disposable == null)
                throw new ArgumentNullException(nameof(disposable));
            _disposable = disposable;
            _inner = inner;
        }
        public override void Flush()
        {
            _inner.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _inner.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _inner.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _inner.Write(buffer, offset, count);
        }

        public override void Close()
        {
            _inner.Close();
            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                _inner.Close();
            }
            catch { }
            _inner.Dispose();
            _disposable.Dispose();
            base.Dispose(disposing);
        }
    }
}
