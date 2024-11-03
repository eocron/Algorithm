using System.Collections.Generic;
using Eocron.Algorithms.EqualityComparers;

namespace Eocron.Algorithms.HashCode;

public class HashBytesEqualityComparer : IEqualityComparer<HashBytes>
{
    public bool Equals(HashBytes x, HashBytes y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return ByteArrayEqualityComparer.Default.Equals(x.Value, y.Value);
    }

    public int GetHashCode(HashBytes obj)
    {
        return obj == null ? 0 : ByteArrayEqualityComparer.Default.GetHashCode(obj.Value);
    }

    public static readonly HashBytesEqualityComparer Default = new();
}