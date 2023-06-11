using System;
using System.IO;
using Eocron.Algorithms.Randoms;

namespace Eocron.Algorithms.Tests.Core
{
    internal sealed class AssertStream : Stream
    {
        public AssertStream(long size, int seed)
        {
            Inner = new Random(seed).NextStream(size);
        }

        public override void Close()
        {
            Closed = true;
            Inner.Close();
            base.Close();
        }

        public override void Flush()
        {
            Inner.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Inner.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Inner.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            Inner.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            Inner.Dispose();
            base.Dispose(disposing);
        }

        public override bool CanRead => Inner.CanRead;
        public override bool CanSeek => Inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => Inner.Length;

        public override long Position
        {
            get => Inner.Position;
            set => Inner.Position = value;
        }

        public readonly Stream Inner;
        public bool Closed;
        public bool Disposed;
    }
}