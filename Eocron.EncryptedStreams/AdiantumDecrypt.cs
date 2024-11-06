using System;

namespace Eocron.EncryptedStreams
{
    public sealed class AdiantumDecrypt : AdiantumCryptoTransformBase
    {
        public AdiantumDecrypt(byte[] key) : base(key)
        {
        }

        public override int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            var input = new ArraySegment<byte>(inputBuffer, inputOffset, inputCount);
            var output = new ArraySegment<byte>(outputBuffer, outputOffset, outputBuffer.Length - outputOffset);
            Validate(input, output);
            
            var cl = input.Slice(0, input.Count - 16);
            var cr = input.Slice(input.Count - 16);
            var cm = Add(cr, Hash(cl));
            var pl = StreamXor(cm, cl);
            var pm = Decrypt(cm);
            var pr = Subtract(pm, Hash(pl));
            output.Write(pl).Write(pr);
            return OutputBlockSize;
        }
    }
}