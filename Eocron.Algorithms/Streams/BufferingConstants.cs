using System.Buffers;
// ReSharper disable StaticMemberInGenericType

namespace Eocron.Algorithms.Streams
{
    public class BufferingConstants<T>
    {
        public static readonly int DefaultBufferSize = 8 * 1024;
        public static readonly MemoryPool<T> DefaultMemoryPool = MemoryPool<T>.Shared;
    }
}