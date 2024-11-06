using System;
using System.Numerics;
using System.Security.Cryptography;

namespace Eocron.EncryptedStreams
{
    public abstract class AdiantumCryptoTransformBase : ICryptoTransform
    {
        public ArraySegment<byte> Tweak { get; set; }
        private readonly SymmetricAlgorithm _symmetricAlgorithm;
        private readonly ChaCha20Poly1305 _hashAlgorithm;

        protected AdiantumCryptoTransformBase(byte[] key)
        {
            _symmetricAlgorithm = Aes.Create();
            _symmetricAlgorithm.KeySize = 32;
            _hashAlgorithm = new ChaCha20Poly1305(key);
        }
        
        protected byte[] Encrypt(ArraySegment<byte> data)
        {
            var blockKey = GetBlockKey();
            _symmetricAlgorithm.Key = blockKey;
            var result = new byte[data.Count];
            using var t = _symmetricAlgorithm.CreateEncryptor();
            t.TransformBlock(data.Array, data.Offset, data.Count, result, 0);
            return result;
        }
        
        protected byte[] Decrypt(ArraySegment<byte> data)
        {
            var blockKey = GetBlockKey();
            _symmetricAlgorithm.Key = blockKey;
            var result = new byte[data.Count];
            using var t = _symmetricAlgorithm.CreateDecryptor();
            t.TransformBlock(data.Array, data.Offset, data.Count, result, 0);
            return result;
        }

        private byte[] GetBlockKey()
        {
            throw new NotImplementedException();
        }

        protected byte[] Add(ArraySegment<byte> a, ArraySegment<byte> b)
        {
            var aa = new BigInteger(a, isUnsigned: true, isBigEndian: false);
            var bb = new BigInteger(b, isUnsigned: true, isBigEndian: false);
            return (aa + bb).ToByteArray();
        }

        protected byte[] Hash(ArraySegment<byte> data)
        {
            throw new NotImplementedException();
        }
        
        protected byte[] StreamXor(ArraySegment<byte> nonce, ArraySegment<byte> msg)
        {
            throw new NotImplementedException();
        }
        
        protected byte[] Subtract(ArraySegment<byte> a, ArraySegment<byte> b)
        {
            var aa = new BigInteger(a, isUnsigned: true, isBigEndian: false);
            var bb = new BigInteger(b, isUnsigned: true, isBigEndian: false);
            return (aa - bb).ToByteArray();
        }

        protected void Validate(ArraySegment<byte> input, ArraySegment<byte> output)
        {
            if (input.Count != InputBlockSize)
                throw new ArgumentOutOfRangeException(nameof(input),
                    $"Invalid input size, should be {InputBlockSize} bytes.");
            
            if (output.Count != OutputBlockSize)
                throw new ArgumentOutOfRangeException(nameof(input),
                    $"Invalid output size, should be {OutputBlockSize} bytes.");
        }

        public abstract int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer,
            int outputOffset);

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            return Array.Empty<byte>();
        }

        public bool CanReuseTransform => true;
        public bool CanTransformMultipleBlocks => true;
        public int InputBlockSize => 4096;
        public int OutputBlockSize => 4096;

        public void Dispose()
        {
            _symmetricAlgorithm?.Dispose();
            _hashAlgorithm?.Dispose();
        }
    }
}