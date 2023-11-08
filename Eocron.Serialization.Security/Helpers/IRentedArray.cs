using System;

namespace Eocron.Serialization.Security.Helpers
{
    public interface IRentedArray<out T> : IDisposable
    {
        public T[] Data { get; }
    }
}