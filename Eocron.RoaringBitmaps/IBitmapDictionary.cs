namespace Eocron.RoaringBitmaps
{
    public interface IBitmapDictionary<TKey> : IReadOnlyBitmapDictionary<TKey>, IWriteOnlyBitmapDictionary<TKey>
    {
    }
}