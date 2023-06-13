using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.RoaringBitmaps
{
    public sealed class BitmapDictionary<TKey> : IBitmapDictionary<TKey>
    {
        private readonly IDictionary<TKey, Bitmap> _bitmaps;

        public BitmapDictionary():this(EqualityComparer<TKey>.Default){}
        public BitmapDictionary(IEqualityComparer<TKey> comparer)
        {
            _bitmaps = new Dictionary<TKey, Bitmap>(comparer ?? throw new ArgumentNullException(nameof(comparer)));
        }

        public BitmapDictionary(IEnumerable<KeyValuePair<TKey, Bitmap>> pairs, IEqualityComparer<TKey> comparer = null)
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
                return bitmap.IsEmpty || exists.Intersects(bitmap);
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
            exists.Or(bitmap);
        }

        public void AddOrUpdate(TKey key, params uint[] indexes)
        {
            if(indexes == null || indexes.Length == 0)
                return;
            
            if (!_bitmaps.TryGetValue(key, out var exists))
            {
                exists = new Bitmap();
                _bitmaps[key] = exists;
            }

            exists.AddMany(indexes);
        }

        public bool TryRemove(TKey key, params uint[] indexes)
        {
            if(indexes == null || indexes.Length == 0)
                return false;
            
            if (!_bitmaps.TryGetValue(key, out var exists))
            {
                return false;
            }

            exists.RemoveMany(indexes);
            if (exists.IsEmpty)
            {
                _bitmaps.Remove(key);
            }
            return true;
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

            exists.AndNot(bitmap);
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
            r.OrMany(found);
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
            return _bitmaps.Select(kv => new KeyValuePair<TKey, Bitmap>(kv.Key, (Bitmap)kv.Value.Clone())).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public int Count => _bitmaps.Count;
    }
}