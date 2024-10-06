using System;

namespace Eocron.Serialization.Security.Helpers
{
    public interface IRentedArray<T> : IDisposable
    {
        public Span<T> Data { get; }
    }
}