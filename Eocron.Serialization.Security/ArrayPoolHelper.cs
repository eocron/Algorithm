using System;
using System.Buffers;
using System.Security;

namespace Eocron.Serialization.Security
{
    public static class ArrayPoolHelper
    {
        public static RentedByteArray Rent(ArrayPool<byte> pool, int size)
        {
            if (size <= 0)
            {
                throw new SecurityException("Invalid rent size.");
            }
            return new RentedByteArray(pool.Rent(size), size, pool);
        }
    
        public sealed class RentedByteArray : IDisposable
        {
            private readonly ArrayPool<byte> _pool;
            public readonly ArraySegment<byte> Segment;

            public RentedByteArray(byte[] original, int size, ArrayPool<byte> pool)
            {
                _pool = pool;
                Segment = new ArraySegment<byte>(original, 0, size);
            }

            public void Dispose()
            {
                _pool.Return(Segment.Array, true);
            }
        }
    }
}