namespace Eocron.Serialization.Security.Helpers
{
    public interface IRentedArrayPool<out T>
    {
        IRentedArray<T> RentExact(int size);
    }
}