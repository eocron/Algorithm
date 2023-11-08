using System;
using System.Buffers;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Eocron.Serialization.Security
{
    public sealed class RsaAes256GcmSerializationConverter : ISerializationConverter
    {
        private readonly ISerializationConverter _inner;
        private readonly X509Certificate2 _cert;
        private readonly RSAEncryptionPadding _padding;
        private readonly ArrayPool<byte> _pool;

        public RsaAes256GcmSerializationConverter(ISerializationConverter inner, X509Certificate2 cert, ArrayPool<byte> pool = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _cert = cert ?? throw new ArgumentNullException(nameof(cert));
            _padding = RSAEncryptionPadding.Pkcs1;
            _pool = pool ?? ArrayPool<byte>.Shared;
        }

        public object DeserializeFrom(Type type, StreamReader sourceStream)
        {
            if (!_cert.HasPrivateKey)
            {
                throw new SecurityException("RSA certificate should have private key initialized to perform deserialization.");
            }
            using var rsa = _cert.GetRSAPrivateKey();
            var br = new BinaryReader(sourceStream.BaseStream);
            var encryptedKey = br.ReadBytes(rsa.KeySize / 8);
            var key = rsa.Decrypt(encryptedKey, _padding);
            var converter = new Aes256GcmSerializationConverter(_inner, key, _pool);
            return converter.DeserializeFrom(type, sourceStream);
        }

        public void SerializeTo(Type type, object obj, StreamWriter targetStream)
        {
            using var rsa = _cert.GetRSAPublicKey();
            var key = PasswordDerivationHelper.CreateRandomBytes(Aes256GcmSerializationConverter.KeyByteSize);
            var encryptedKey = rsa.Encrypt(key, _padding);
            var bw = new BinaryWriter(targetStream.BaseStream);
            bw.Write(encryptedKey);
            bw.Flush();
            var converter = new Aes256GcmSerializationConverter(_inner, key, _pool);
            converter.SerializeTo(type, obj, targetStream);
        }
    }
}