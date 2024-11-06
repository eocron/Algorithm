using System;
using System.Linq;

namespace Eocron.EncryptedStreams
{
    public sealed class AdiantumEncrypt : AdiantumCryptoTransformBase
    {
        public AdiantumEncrypt(byte[] key) : base(key)
        {
        }

        public override int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            var input = new ArraySegment<byte>(inputBuffer, inputOffset, inputCount);
            var output = new ArraySegment<byte>(outputBuffer, outputOffset, outputBuffer.Length - outputOffset);
            Validate(input, output);

            var pl = input.Slice(0, input.Count - 16);
            var pr = input.Slice(input.Count - 16);
            var pm = Add(pr, Hash(pl));
            var cm = Encrypt(pm);
            var cl = StreamXor(cm, pl);
            var cr = Subtract(cm, Hash(cl));
            output.Write(cl).Write(cr);
            return OutputBlockSize;
        }
    }
}