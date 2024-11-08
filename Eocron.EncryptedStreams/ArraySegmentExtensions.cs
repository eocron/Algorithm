using System;

namespace Eocron.EncryptedStreams
{
    public static class ArraySegmentExtensions
    {
        public static void Copy(this ArraySegment<byte> src, byte[] dst, int dstOffset, int count)
        {
            Buffer.BlockCopy(src.Array, src.Offset, dst, dstOffset, count);
        }
        
        public static void Copy(this ArraySegment<byte> src, int srcOffset, byte[] dst, int dstOffset, int count)
        {
            Buffer.BlockCopy(src.Array, src.Offset + srcOffset, dst, dstOffset, count);
        }
    }
}