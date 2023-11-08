using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Eocron.Serialization.Security
{
    public static class ArrayPoolHelper
    {
        /// <summary>
        /// This method will return disposable object which contain desired size T[] buffer.
        /// Works with any pool which supports desired size.
        /// </summary>
        /// <param name="pool">Array pool</param>
        /// <param name="size">Desired size</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static IRentedArray<T> RentExact<T>(ArrayPool<T> pool, int size)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            var rented = pool.Rent(size);
            Debug.Assert(rented.Length >= size, "rented.Length <= size");
            var originalSize = UnsafeChangeLength(rented, (uint)size);
            return new RentedByteArray<T>(rented, originalSize, pool);
        }

        private static uint UnsafeChangeLength<T>(T[] array, uint newLength)
        {
            if (array.Length == newLength)
                return newLength;
            
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