using System.Collections.Generic;
using CRoaring;

namespace Eocron.RoaringBitmaps
{
    public interface IReadOnlyBitmap : IEnumerable<uint>
    {
        bool IsEmpty { get; }
        uint Min { get; }
        uint Max { get; }
        bool Contains(uint value);
        bool IsSubset(Bitmap bitmap, bool isStrict = false);
        bool Select(uint rank, out uint element);
        Bitmap Not(ulong start, ulong end);
        Bitmap And(Bitmap bitmap);
        Bitmap AndNot(Bitmap bitmap);
        Bitmap Or(Bitmap bitmap);
        Bitmap Xor(Bitmap bitmap);
        bool Intersects(Bitmap bitmap);
        Statistics GetStatistics();
    }
}