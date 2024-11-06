using System;

namespace Eocron.EncryptedStreams
{
    public static class ArraySegmentExtensions
    {
        public static ArraySegment<byte> Write(this ArraySegment<byte> destination, ArraySegment<byte> source)
        {
            Array.Copy(source.Array, source.Offset, destination.Array, destination.Offset, source.Count);
            var newOffset = destination.Offset + source.Count;
            var newCount = destination.Count - source.Count;
            return new ArraySegment<byte>(destination.Array, newOffset, newCount);
        }
    }
}