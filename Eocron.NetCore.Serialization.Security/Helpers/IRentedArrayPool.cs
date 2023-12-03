namespace Eocron.NetCore.Serialization.Security.Helpers
{
    public interface IRentedArrayPool<T>
    {
        IRentedArray<T> RentExact(int size);
    }
}