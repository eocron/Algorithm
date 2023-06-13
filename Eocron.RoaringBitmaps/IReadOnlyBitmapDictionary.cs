using System.Collections.Generic;

namespace Eocron.RoaringBitmaps
{
    public interface IReadOnlyBitmapDictionary<TKey> : IEnumerable<KeyValuePair<TKey, Bitmap>>
    {
        int Count { get; }
        Bitmap TryGet(params TKey[] keys);
        bool Contains(TKey key);
        bool Contains(TKey key, Bitmap bitmap);
    }
}