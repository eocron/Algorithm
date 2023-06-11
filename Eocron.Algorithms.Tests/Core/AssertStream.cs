using System;
using System.IO;
using Eocron.Algorithms.Randoms;

namespace Eocron.Algorithms.Tests.Core
{
    internal sealed class AssertStream : Stream
    {
        public readonly Stream Inner;
        public bool Disposed;
        public bool Closed;

        public AssertStream(long size, int seed)
        {
            Inner = new Random(seed).NextStream(size);
        }

        public override void Flush()
        {
            Inner.Flush();
        }

        public override void Close()
        {
            Closed = true;
            Inner.Close();
            base.Close();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Inner.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            Inner.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Inner.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead => Inner.CanRead;
        public override bool CanSeek => Inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => Inner.Length;

        public override long Position { get => Inner.Position; set => Inner.Position = value; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            Inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
