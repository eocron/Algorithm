using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.RoaringBitmaps
{
    public sealed class BitmapArray<TKey> : IBitmapArray<TKey>
    {
        private readonly Dictionary<TKey, Bitmap> _bitmaps;

        public BitmapArray():this(EqualityComparer<TKey>.Default){}
        public BitmapArray(IEqualityComparer<TKey> comparer)
        {
            _bitmaps = new Dictionary<TKey, Bitmap>(comparer);
        }

        public BitmapArray(IEnumerable<KeyValuePair<TKey, Bitmap>> pairs, IEqualityComparer<TKey> comparer = null)
        {
            _bitmaps = pairs.ToDictionary(x=> x.Key, x=> (Bitmap)x.Value.Clone(), comparer);
        } 

        public bool Contains(TKey key)
        {
            return _bitmaps.ContainsKey(key);
        }

        public bool Contains(TKey key, Bitmap bitmap)
        {
            if (_bitmaps.TryGetValue(key, out var exists))
            {
                return bitmap.IsEmpty || exists.And(bitmap).IsEmpty;
            }
            return false;
        }

        public void AddOrUpdate(TKey key, Bitmap bitmap)
        {
            if(bitmap.IsEmpty)
                return;
            
            if (!_bitmaps.TryGetValue(key, out var exists))
            {
                exists = new Bitmap();
                _bitmaps[key] = exists;
            }
            exists.IOr(bitmap);
        }

        public bool TryRemove(TKey key)
        {
            return _bitmaps.Remove(key);
        }

        public void Clear()
        {
            _bitmaps.Clear();
        }

        public bool TryRemove(TKey key, Bitmap bitmap)
        {
            if (bitmap.IsEmpty)
                return false;
            
            if (!_bitmaps.TryGetValue(key, out var exists))
            {
                return false;
            }

            exists.IAndNot(bitmap);
            if (exists.IsEmpty)
            {
                _bitmaps.Remove(key);
            }
            return true;
        }

        public Bitmap TryGet(params TKey[] keys)
        {
            var found = keys.Select(GetOrDefault).Where(x => x != null).ToList();
            if (found.Count == 0)
                return new Bitmap();
            if (found.Count == 1)
                return (Bitmap)found[0].Clone();
            
            var r = new Bitmap();
            r.IOrMany(found);
            return r;
        }

        private Bitmap GetOrDefault(TKey key)
        {
            if (_bitmaps.TryGetValue(key, out var res))
            {
                return res;
            }
            return null;
        }

        public IEnumerator<KeyValuePair<TKey, Bitmap>> GetEnumerator()
        {
            return _bitmaps.Select(kv => new KeyValuePair<TKey, Bitmap>(kv.Key, kv.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}