using System;

namespace Eocron.Serialization.Security.Helpers
{
    public sealed class NonRentedArrayPool<T> : IRentedArrayPool<T>
    {
        public static readonly IRentedArrayPool<T> Shared = new NonRentedArrayPool<T>();
        
        public IRentedArray<T> RentExact(int size)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size));
            return new NonRentedArray(size);
        }
        
        private sealed class NonRentedArray : IRentedArray<T>
        {
            private readonly T[] _data;
            public NonRentedArray(int size)
            {
                _data = new T[size];
            }

            public void Dispose()
            {
                Array.Clear(_data, 0, _data.Length);
            }

            public T[] Data => _data;
        }
    }
}