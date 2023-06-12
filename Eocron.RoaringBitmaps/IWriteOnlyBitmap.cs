using System.Collections.Generic;

namespace Eocron.RoaringBitmaps
{
    public interface IWriteOnlyBitmap
    {
        void Add(uint value);
        void AddMany(params uint[] values);
        void AddMany(uint[] values, uint offset, uint count);
        void Remove(uint value);
        void RemoveMany(params uint[] values);
        void RemoveMany(uint[] values, uint offset, uint count);

        void INot(ulong start, ulong end);
        void IAnd(Bitmap bitmap);
        void IAndNot(Bitmap bitmap);
        void IOr(Bitmap bitmap);
        void IOrMany(IEnumerable<Bitmap> bitmaps);
        void IXor(Bitmap bitmap);
    }
}