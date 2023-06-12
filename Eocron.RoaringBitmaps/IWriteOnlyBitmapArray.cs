namespace Eocron.RoaringBitmaps
{
    public interface IWriteOnlyBitmapArray<in TKey>
    {
        void AddOrUpdate(TKey key, Bitmap bitmap);
        bool TryRemove(TKey key, Bitmap bitmap);
        bool TryRemove(TKey key);

        void Clear();
    }
}