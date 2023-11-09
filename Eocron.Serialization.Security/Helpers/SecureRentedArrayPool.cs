namespace Eocron.Serialization.Security.Helpers
{
    public sealed class SecureRentedArrayPool<T> : IRentedArrayPool<T>
    {
        public static readonly IRentedArrayPool<T> Shared = new SecureRentedArrayPool<T>();
        
        public IRentedArray<T> RentExact(int size)
        {
            return new NonRentedArray(size);
        }
        
        private sealed class NonRentedArray : IRentedArray<T>
        {
            public NonRentedArray(int size)
            {
                Data = new T[size];
            }

            public void Dispose()
            {
                for (int i = 0; i < Data.Length; i++)
                {
                    Data[i] = default;
                }
            }

            public T[] Data { get; }
        }
    }
}