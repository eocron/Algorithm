using System;
using System.Runtime.Serialization;
using Eocron.Algorithms.Hex;

namespace Eocron.Algorithms.HashCode;

[Serializable]
[DataContract]
public sealed class HashBytes : IEquatable<HashBytes>, ICloneable
{
    public byte[] Value { get; set; }
    public string Source { get; set; }

    public bool Equals(HashBytes other)
    {
        return HashBytesEqualityComparer.Default.Equals(this, other);
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((HashBytes)obj);
    }

    public override int GetHashCode()
    {
        return HashBytesEqualityComparer.Default.GetHashCode(this);
    }

    public object Clone()
    {
        return new HashBytes
        {
            Source = Source,
            Value = (byte[])Value.Clone()
        };
    }

    public static bool operator ==(HashBytes left, HashBytes right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(HashBytes left, HashBytes right)
    {
        return !Equals(left, right);
    }

    public override string ToString()
    {
        return Value?.ToHexString();
    }
}