using System;
using System.Collections;
using System.Collections.Generic;
using CRoaring;

namespace Eocron.RoaringBitmaps
{
    public sealed class Bitmap : ICloneable, IEquatable<Bitmap>, IEnumerable<uint>
    {
        private readonly RoaringBitmap _inner;

        public byte[] ToByteArray()
        {
            return _inner.Serialize(SerializationFormat.Portable);
        }
        
        public Bitmap(uint size)
        {
            _inner = new RoaringBitmap(size);
        }

        public Bitmap()
        {
            _inner = new RoaringBitmap();
        }
        
        public Bitmap(Bitmap other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            _inner = (RoaringBitmap)other._inner.Clone();
        }

        public Bitmap(byte[] data)
        {
            _inner = RoaringBitmap.Deserialize(data ?? throw new ArgumentNullException(nameof(data)), SerializationFormat.Portable);
        }

        private Bitmap(RoaringBitmap inner)
        {
            _inner = inner;
        }

        public IEnumerator<uint> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        ~Bitmap()
        {
            _inner.Dispose();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool IsEmpty => _inner.IsEmpty;

        public uint Min => _inner.Min;

        public uint Max => _inner.Max;

        public void Add(uint value)
        {
            _inner.Add(value);
        }

        public void AddMany(params uint[] values)
        {
            _inner.AddMany(values);
        }

        public void AddMany(uint[] values, uint offset, uint count)
        {
            _inner.AddMany(values, offset, count);
        }

        public void Remove(uint value)
        {
            _inner.Remove(value);
        }

        public void RemoveMany(params uint[] values)
        {
            _inner.RemoveMany(values);
        }

        public void RemoveMany(uint[] values, uint offset, uint count)
        {
            _inner.RemoveMany(values, offset, count);
        }

        public bool Contains(uint value)
        {
            return _inner.Contains(value);
        }

        public bool IsSubset(Bitmap bitmap, bool isStrict = false)
        {
            return _inner.IsSubset(bitmap._inner, isStrict);
        }

        public bool Select(uint rank, out uint element)
        {
            return _inner.Select(rank, out element);
        }

        public Bitmap Not(ulong start, ulong end)
        {
            return new Bitmap(_inner.Not(start, end));
        }

        public void INot(ulong start, ulong end)
        {
            _inner.INot(start, end);
        }

        public Bitmap And(Bitmap bitmap)
        {
            return new Bitmap(_inner.And(bitmap._inner));
        }

        public void IAnd(Bitmap bitmap)
        {
            _inner.IAnd(bitmap._inner);
        }
        
        public Bitmap AndNot(Bitmap bitmap)
        {
            return new Bitmap(_inner.AndNot(bitmap._inner));
        }

        public void IAndNot(Bitmap bitmap)
        {
            _inner.IAndNot(bitmap._inner);
        }
        
        public Bitmap Or(Bitmap bitmap)
        {
            return new Bitmap(_inner.Or(bitmap._inner));
        }

        public void IOr(Bitmap bitmap)
        {
            _inner.IOr(bitmap._inner);
        }

        public void IOrMany(IEnumerable<Bitmap> bitmaps)
        {
            foreach (var bm in bitmaps)
            {
                _inner.ILazyOr(bm._inner, false);
            }
            _inner.RepairAfterLazy();
        }

        public Bitmap Xor(Bitmap bitmap)
        {
            return new Bitmap(_inner.Xor(bitmap._inner));
        }

        public void IXor(Bitmap bitmap)
        {
            _inner.IXor(bitmap._inner);
        }
        
        public bool Intersects(Bitmap bitmap)
        {
            return _inner.Intersects(bitmap._inner);
        }

        public Statistics GetStatistics()
        {
            return _inner.GetStatistics();
        }

        public object Clone()
        {
            return new Bitmap(_inner.Clone());
        }

        public bool Equals(Bitmap other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(_inner, other._inner);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is Bitmap other && Equals(other);
        }

        public override int GetHashCode()
        {
            if (_inner == null)
                return 0;

            return System.HashCode.Combine(_inner.Cardinality, _inner.Min, _inner.Max);
        }

        public static bool operator ==(Bitmap left, Bitmap right)
        {
            return Equals(left, right);
        }
        
        public static Bitmap operator |(Bitmap left, Bitmap right)
        {
            return new Bitmap(left._inner.Or(right._inner));
        }
        
        public static Bitmap operator &(Bitmap left, Bitmap right)
        {
            return new Bitmap(left._inner.And(right._inner));
        }
        
        public static Bitmap operator ^(Bitmap left, Bitmap right)
        {
            return new Bitmap(left._inner.Xor(right._inner));
        }

        public static bool operator !=(Bitmap left, Bitmap right)
        {
            return !Equals(left, right);
        }
    }
}