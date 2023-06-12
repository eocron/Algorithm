namespace Eocron.RoaringBitmaps
{
    public interface IBitmapArray<TKey> : IReadOnlyBitmapArray<TKey>, IWriteOnlyBitmapArray<TKey>
    {
    }
}