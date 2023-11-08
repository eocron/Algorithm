using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Security;

namespace Eocron.Serialization.Security
{
    public static class ArrayPoolHelper
    {
        public static IRentedArray<T> Rent<T>(ArrayPool<T> pool, int size)
        {
            if (size <= 0)
            {
                throw new SecurityException("Invalid rent size.");
            }

            var rented = pool.Rent(size);
            var originalSize = UnsafeChangeLength(rented, (uint)size);
            return new RentedByteArray<T>(rented, originalSize, pool);
        }

        private static uint UnsafeChangeLength<T>(T[] array, uint newLength)
        {
            var unsafeRented = Unsafe.As<RawArrayData>(array);
            var originalLength = unsafeRented.Length;
            unsafeRented.Length = newLength;
            return originalLength;
        }

        private sealed class RentedByteArray<T> : IRentedArray<T>
        {
            private readonly uint _originalSize;
            private readonly ArrayPool<T> _pool;
            private readonly T[] _original;
            private bool _disposed;
            public T[] Data => _disposed ? throw new ObjectDisposedException(this.ToString()) : _original;

            public RentedByteArray(T[] original, uint originalSize, ArrayPool<T> pool)
            {
                _originalSize = originalSize;
                _pool = pool;
                _original = original;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;
                UnsafeChangeLength(_original, _originalSize);
                _pool.Return(_original, true);
                _disposed = true;
            }
        }

        private sealed class RawArrayData {
            public uint Length;
        }
    }
}