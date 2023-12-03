using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Eocron.NetCore.Serialization.Security.Helpers;
using Eocron.Serialization;

namespace Eocron.NetCore.Serialization.Security
{
    /// <summary>
    /// Asymmetric encryption identical to RSA-AES256-GCM-SHA512 cipher suit.
    /// Encrypt/Serialize with public part of certificate.
    /// Decrypt/Deserialize with private part of certificate.
    /// Useful when you have multiple serialization holders/nodes/jobs/etc and single deserializer/reader.
    /// </summary>
    public sealed class AsymmetricEncryptionSerializationConverter : ISerializationConverter
    {
        private readonly ISerializationConverter _inner;
        private readonly X509Certificate2 _cert;
        private readonly RSAEncryptionPadding _padding;
        private readonly IRentedArrayPool<byte> _pool;

        public AsymmetricEncryptionSerializationConverter(ISerializationConverter inner, X509Certificate2 cert, IRentedArrayPool<byte> pool = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _cert = cert ?? throw new ArgumentNullException(nameof(cert));
            _padding = RSAEncryptionPadding.OaepSHA512;
            _pool = pool ?? RentedArrayPool<byte>.Shared;
        }

        public object DeserializeFrom(Type type, StreamReader sourceStream)
        {
            if (!_cert.HasPrivateKey)
            {
                throw new SecurityException("RSA certificate should have private key initialized to perform deserialization.");
            }
            using var rsa = _cert.GetRSAPrivateKey();
            using var encryptedKey = _pool.RentExact(rsa.KeySize / 8);
            
            var br = new BinaryReader(sourceStream.BaseStream);
            br.ReadExactly(encryptedKey);
            var key = rsa.Decrypt(encryptedKey.Data, _padding);
            var converter = new SymmetricEncryptionSerializationConverter(_inner, key, _pool);
            return converter.DeserializeFrom(type, sourceStream);
        }

        public void SerializeTo(Type type, object obj, StreamWriter targetStream)
        {
            using var rsa = _cert.GetRSAPublicKey();
            using var key = PasswordDerivationHelper.CreateRandomBytes(_pool, SymmetricEncryptionSerializationConverter.KeyByteSize);
            
            var bw = new BinaryWriter(targetStream.BaseStream);
            bw.Write(rsa.Encrypt(key.Data, _padding));
            bw.Flush();
            var converter = new SymmetricEncryptionSerializationConverter(_inner, key.Data, _pool);
            converter.SerializeTo(type, obj, targetStream);
        }
    }
}