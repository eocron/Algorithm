using System;
using System.IO;
namespace Tests
{
    internal sealed class TestStream : Stream
    {
        private readonly long _size;
        private readonly long _seed;

        public bool Disposed;
        public bool Closed;

        private static byte GetByte(long position, long seed)
        {
            var h = seed;
            h ^= 12345;
            h += position;
            h ^= 3345;
            return (byte)h;
        }

        public TestStream(long size, long seed)
        {
            _size = size;
            _seed = seed;
        }

        public override void Flush()
        {
        }

        public override void Close()
        {
            Closed = true;
            base.Close();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {

            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var len = Length;
            var pos = Position;

            var left = Math.Min(count, len - pos);
            for (int i = 0; i < left; i++)
            {
                buffer[i] = GetByte(pos + i, _seed);
            }

            Position += left;
            return (int)left;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _size;
        public override long Position { get; set; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
