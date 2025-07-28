using System;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.DependencyInjection.Interceptors.Caching
{
    internal sealed class CompoundKey : IEquatable<CompoundKey>
    {
        private readonly IReadOnlyList<object> _parts;

        public CompoundKey(IReadOnlyList<object> parts)
        {
            _parts = parts ?? throw new ArgumentNullException();
        }

        public bool Equals(CompoundKey other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (_parts == other._parts) return true;
            if (_parts.Count != other._parts.Count) return false;
            return _parts.SequenceEqual(other._parts);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is CompoundKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hc = new HashCode();
            foreach (var part in _parts)
            {
                hc.Add(part);
            }
            return hc.ToHashCode();
        }
    }
}