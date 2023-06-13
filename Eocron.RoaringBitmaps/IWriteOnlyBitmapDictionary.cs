namespace Eocron.RoaringBitmaps
{
    public interface IWriteOnlyBitmapDictionary<in TKey>
    {
        void AddOrUpdate(TKey key, Bitmap bitmap);
        void AddOrUpdate(TKey key, params uint[] indexes);
        bool TryRemove(TKey key, Bitmap bitmap);
        bool TryRemove(TKey key, params uint[] indexes);
        bool TryRemove(TKey key);
        void Clear();
    }
}