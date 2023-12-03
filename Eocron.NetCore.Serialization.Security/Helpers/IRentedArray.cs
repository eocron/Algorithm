using System;

namespace Eocron.NetCore.Serialization.Security.Helpers
{
    public interface IRentedArray<T> : IDisposable
    {
        public Span<T> Data { get; }
    }
}