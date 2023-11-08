using System;
using System.Buffers;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Eocron.Serialization.Security
{
    /// <summary>
    /// Encrypt/Serialize with public part of certificate.
    /// Decrypt/Deserialize with private part of certificate.
    /// Useful when you have multiple serialization holders/nodes/jobs/etc and single deserializer/reader.
    /// </summary>
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
            var key = rsa.Decrypt(br.ReadBytes(rsa.KeySize / 8), _padding);
            var converter = new Aes256GcmSerializationConverter(_inner, key, _pool);
            return converter.DeserializeFrom(type, sourceStream);
        }

        public void SerializeTo(Type type, object obj, StreamWriter targetStream)
        {
            using var rsa = _cert.GetRSAPublicKey();
            using var key = PasswordDerivationHelper.CreateRandomBytes(_pool, Aes256GcmSerializationConverter.KeyByteSize);
            var bw = new BinaryWriter(targetStream.BaseStream);
            bw.Write(rsa.Encrypt(key.Data, _padding));
            bw.Flush();
            var converter = new Aes256GcmSerializationConverter(_inner, key.Data, _pool);
            converter.SerializeTo(type, obj, targetStream);
        }
    }
}