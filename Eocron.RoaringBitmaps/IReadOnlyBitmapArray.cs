using System.Collections.Generic;

namespace Eocron.RoaringBitmaps
{
    public interface IReadOnlyBitmapArray<TKey> : IEnumerable<KeyValuePair<TKey, Bitmap>>
    {
        Bitmap TryGet(params TKey[] keys);
        bool Contains(TKey key);
        bool Contains(TKey key, Bitmap bitmap);
    }
}