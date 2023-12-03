using System;
using System.Buffers;

namespace Eocron.NetCore.Serialization.Security.Helpers
{
    public sealed class MemoryRentedArrayPool<T> : IRentedArrayPool<T>
    {
        private readonly MemoryPool<T> _pool;

        public MemoryRentedArrayPool(MemoryPool<T> pool = null)
        {
            _pool = pool ?? MemoryPool<T>.Shared;
        }

        public IRentedArray<T> RentExact(int size)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size));
            return new MemoryRentedArray(size, _pool.Rent(size));
        }
        
        private sealed class MemoryRentedArray : IRentedArray<T>
        {
            private readonly int _size;
            private IMemoryOwner<T> _owner;

            public MemoryRentedArray(int size, IMemoryOwner<T> owner)
            {
                _size = size;
                _owner = owner;
            }

            public void Dispose()
            {
                if(_owner == null)
                    return;
                _owner.Memory.Span.Fill(default);
                _owner.Dispose();
                _owner = null;
            }

            public Span<T> Data => _owner.Memory.Slice(0, _size).Span;
        }
    }
}