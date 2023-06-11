using System;
using System.Buffers;
using System.IO;

namespace Eocron.Algorithms.Randoms
{
    internal sealed class RandomStream : Stream
    {
        private readonly long _seed;
        private readonly int _blockLength;
        private readonly ArrayPool<byte> _pool;

        private long _length;
        private long _position;

        public RandomStream(long seed, long length, int blockLength, ArrayPool<byte> pool)
        {
            _seed = seed;
            _length = length;
            _blockLength = blockLength;
            _pool = pool;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _length;

        public override long Position { 
            get { return _position; } 
            set { _position = value; } }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset < 0 || offset >= buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            var left = Math.Min(count, _length - _position);
            var read = 0;
            if (left <= 0)
                return 0;
            var rndBlock = _pool.Rent(_blockLength);
            try
            {
                int i = 0;
                while (i < left && _position < _length)
                {
                    var blockId = _position / _blockLength;
                    var blockOffset = _position % _blockLength;
                    var rnd = new Random(unchecked((int)(blockId * 31 + _seed)));
                    rnd.NextBytes(rndBlock);
                    for (var j = blockOffset; 
                        j < _blockLength && i < left && _position < _length; 
                        j++, i++, _position++, read++)
                    {
                        buffer[offset + i] = rndBlock[j];
                    }
                }
            }
            finally
            {
                _pool.Return(rndBlock);
            }
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition;
            if (origin == SeekOrigin.Begin)
                newPosition = offset;
            else if (origin == SeekOrigin.End)
                newPosition = offset + _length;
            else
                newPosition = offset + _position;

            if (newPosition < 0 || newPosition > _length)
                throw new ArgumentOutOfRangeException(nameof(offset), newPosition, "Invalid target position");
            _position = newPosition;
            return _position;
        }

        public override void SetLength(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            _length = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
