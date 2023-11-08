using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Eocron.Serialization.Security.Helpers
{
    public static class ArrayPoolHelper
    {
        /// <summary>
        /// This method will return disposable object which contain desired size T[] buffer.
        /// No additional allocations except those in ArrayPool will be issued.
        /// Works with any pool which supports desired size.
        /// </summary>
        /// <param name="pool">Array pool to use</param>
        /// <param name="size">Desired size</param>
        /// <param name="clearOnDispose">Should buffer be cleared upon return to pool</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Disposable rented array</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static IRentedArray<T> RentExact<T>(ArrayPool<T> pool, int size, bool clearOnDispose = true)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            var rented = pool.Rent(size);
            var originalSize = UnsafeChangeLength(rented, (uint)size);
            return new RentedArray<T>(rented, originalSize, pool, clearOnDispose);
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

        private sealed class RentedArray<T> : IRentedArray<T>
        {
            private readonly uint _originalSize;
            private readonly ArrayPool<T> _pool;
            private readonly bool _clearOnDispose;
            private readonly T[] _original;
            private bool _disposed;
            public T[] Data => _disposed ? throw new ObjectDisposedException(ToString()) : _original;

            public RentedArray(T[] original, uint originalSize, ArrayPool<T> pool, bool clearOnDispose)
            {
                _originalSize = originalSize;
                _pool = pool;
                _clearOnDispose = clearOnDispose;
                _original = original;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;
                UnsafeChangeLength(_original, _originalSize);
                _pool.Return(_original, _clearOnDispose);
                _disposed = true;
            }
        }

        private sealed class RawArrayData {
            public uint Length;
        }
    }
}