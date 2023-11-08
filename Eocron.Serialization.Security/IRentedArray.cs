using System;

namespace Eocron.Serialization.Security
{
    public interface IRentedArray<out T> : IDisposable
    {
        public T[] Data { get; }
    }
}