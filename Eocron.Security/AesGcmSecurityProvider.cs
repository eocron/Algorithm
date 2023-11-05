using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Eocron.Security
{
    public class AesGcmEncryptionProvider
    {
        private readonly RandomNumberGenerator _rng = new RNGCryptoServiceProvider();
        private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Create();
        private readonly byte[] _key;
        
        public static class AesGcmConsts
        {
            public static class Tag
            {
                public static int MaxByteSize = 16;
            }
            
            public static class Nonce
            {
                public static int MaxByteSize = 12;
            }
        }

        public AesGcmEncryptionProvider(byte[] key, int dataBlockSize)
        {
            _key = key;
            _pool = ArrayPool<byte>.Shared;
        }

        public void WriteTo(ArraySegment<byte> source, Stream target)
        {
            using var tag = Rent(AesGcmConsts.Tag.MaxByteSize);
            using var nonce = Rent(AesGcmConsts.Nonce.MaxByteSize);
            using var encryptedBytes = Rent(source.Count);
            _rng.GetNonZeroBytes(nonce.Segment.Array);

            var cipher = new GcmBlockCipher(new AesLightEngine());
            cipher.Init(true, new AeadParameters(new KeyParameter(_key), 128, nonce.Segment.Array.));
            //some encryption
            using var bw = new BinaryWriter(target, Encoding.UTF8, leaveOpen: true);
            bw.Write(nonce.Segment.Array, nonce.Segment.Offset, nonce.Segment.Count);
            bw.Write(tag.Segment.Array, tag.Segment.Offset, tag.Segment.Count);
            bw.Write((uint)encryptedBytes.Segment.Count);
            bw.Write(encryptedBytes.Segment.Array, encryptedBytes.Segment.Offset, encryptedBytes.Segment.Count);
        }

        public void ReadFrom(Stream source, ArraySegment<byte> target)
        {
            using var tag = Rent(AesGcmConsts.Tag.MaxByteSize);
            using var nonce = Rent(AesGcmConsts.Nonce.MaxByteSize);
            using var encryptedBytes = Rent(target.Count);
            using var br = new BinaryReader(source, Encoding.UTF8, leaveOpen: true);
            ReadExactly(br, nonce.Segment);
            ReadExactly(br, tag.Segment);
            var size = br.ReadInt32();
            ReadExactly(br, size, encryptedBytes.Segment);
            //some decryption
        }

        private void ReadExactly(BinaryReader source, ArraySegment<byte> buffer)
        {
            if (source.Read(buffer.Array, buffer.Offset, buffer.Count) != buffer.Count)
            {
                throw CreateIntegrityFailedException();
            }
        }
        
        private void ReadExactly(BinaryReader source, int size, ArraySegment<byte> buffer)
        {
            if (size <= 0)
            {
                throw CreateIntegrityFailedException();
            }
            if (source.Read(buffer.Array, buffer.Offset, size) != size)
            {
                throw CreateIntegrityFailedException();
            }
        }

        private Exception CreateIntegrityFailedException()
        {
            return new SecurityException("Integrity check failed.");
        }

        private RentedArray Rent(int size)
        {
            var array = _pool.Rent(size);
            return new RentedArray(array, size, _pool);
        }

        private readonly struct RentedArray : IDisposable
        {
            private readonly byte[] _array;
            private readonly ArrayPool<byte> _pool;
            public readonly ArraySegment<byte> Segment;

            public RentedArray(byte[] array, int size, ArrayPool<byte> pool)
            {
                _array = array;
                _pool = pool;
                Segment = new ArraySegment<byte>(array, 0, size);
            }
            
            public void Dispose()
            {
                _pool.Return(_array);
            }
        }
    }
}