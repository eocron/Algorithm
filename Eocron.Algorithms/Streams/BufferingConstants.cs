using System.Buffers;

namespace Eocron.Algorithms.Streams
{
    public class BufferingConstants<T>
    {
        public static int DefaultBufferSize = 8 * 1024;
        public static MemoryPool<T> DefaultMemoryPool = MemoryPool<T>.Shared;
    }
}