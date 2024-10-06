namespace Eocron.Serialization.Security.Helpers
{
    public static class RentedArrayPool<T>
    {
        public static readonly IRentedArrayPool<T> Shared = new MemoryRentedArrayPool<T>();
    }
}