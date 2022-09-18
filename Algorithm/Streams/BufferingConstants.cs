using System.Buffers;

namespace Eocron.Algorithms.Streams
{
    public class BufferingConstants<T>
    {
        public static MemoryPool<T> DefaultMemoryPool = MemoryPool<T>.Shared;
        public static int DefaultBufferSize = 8 * 1024;
    }
}