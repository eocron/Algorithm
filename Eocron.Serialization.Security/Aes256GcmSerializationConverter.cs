using System;
using System.Buffers;
using System.IO;
using System.Security;
using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Eocron.Serialization.Security;

/// <summary>
/// Used for general encryption where same secret is shared between writers/readers.
/// AES256 GCM provide two important features: decent security, integrity validation.
/// </summary>
public sealed class Aes256GcmSerializationConverter : BinarySerializationConverterBase
{
    private const int NonceByteSize = 12;
    public const int KeyByteSize = 32;
    private const int MacByteSize = 16;
    private const int MacBitSize = MacByteSize * 8;
    
    private readonly ISerializationConverter _inner;
    private readonly ArrayPool<byte> _arrayPool;
    private readonly byte[] _key;

    public Aes256GcmSerializationConverter(ISerializationConverter inner, string password, ArrayPool<byte> pool = null) :
        this(inner, PasswordDerivationHelper.GenerateKeyFrom(password, KeyByteSize), pool)
    {
    }

    public Aes256GcmSerializationConverter(ISerializationConverter inner, byte[] key, ArrayPool<byte> pool = null)
    {
        if (inner == null)
            throw new ArgumentNullException(nameof(inner));
        if (key == null || key.Length == 0)
            throw new ArgumentNullException(nameof(key));
        if (key.Length != KeyByteSize)
            throw new ArgumentOutOfRangeException(nameof(key), $"Key should be of size: {KeyByteSize}");
        
        _inner = inner;
        _arrayPool = pool ?? ArrayPool<byte>.Shared;
        _key = key;
    }

    protected override object DeserializeFrom(Type type, BinaryReader reader)
    {
        using var body = ReadAesGcmData(reader);
        var cipher = CreateAeadCipher(body.Nonce, false);
        using var decryptedPayload = ArrayPoolHelper.RentExact(_arrayPool, cipher.GetOutputSize(body.EncryptedPayload.Data.Length));

        var len = cipher.ProcessBytes(
            body.EncryptedPayload.Data,
            0,
            body.EncryptedPayload.Data.Length,
            decryptedPayload.Data,
            0);
        cipher.DoFinal(decryptedPayload.Data, len);
        using var ms = new MemoryStream(decryptedPayload.Data, 0, decryptedPayload.Data.Length, false);
        return _inner.DeserializeFrom(type, ms);
    }

    protected override void SerializeTo(Type type, object obj, BinaryWriter writer)
    {
        var decryptedPayload = new ArraySegment<byte>(_inner.SerializeToBytes(type, obj, Encoding.UTF8));
        using var nonce = PasswordDerivationHelper.CreateRandomBytes(_arrayPool, NonceByteSize);
        using var encrypted = ArrayPoolHelper.RentExact(_arrayPool, decryptedPayload.Count + MacByteSize);
        using var body = new RentedAesGcmData(nonce, encrypted);
        var cipher = CreateAeadCipher(body.Nonce, true);
        var len = cipher.ProcessBytes(
            decryptedPayload.Array,
            decryptedPayload.Offset,
            decryptedPayload.Count,
            body.EncryptedPayload.Data,
            0);
        cipher.DoFinal(body.EncryptedPayload.Data, len);
        
        WriteAesGcmData(writer, body);
    }


    
    private IAeadCipher CreateAeadCipher(IRentedArray<byte> nonce, bool forEncryption)
    {
        var cipher = new GcmBlockCipher(new AesLightEngine());
        var parameters = new AeadParameters(new KeyParameter(_key), MacBitSize, nonce.Data);
        cipher.Init(forEncryption, parameters);
        return cipher;
    }

    private void ReadExactly(BinaryReader reader, IRentedArray<byte> segment)
    {
        var read = reader.Read(segment.Data, 0, segment.Data.Length);
        if (read != segment.Data.Length)
        {
            throw new SecurityException("Integrity check failed. Amount of read bytes doesn't match expected.");
        }
    }

    private RentedAesGcmData ReadAesGcmData(BinaryReader reader)
    {
        var nonce = ArrayPoolHelper.RentExact(_arrayPool, NonceByteSize);
        try
        {
            ReadExactly(reader, nonce);
            var encryptedPayloadSize = reader.ReadInt32();
            var encryptedPayload = ArrayPoolHelper.RentExact(_arrayPool, encryptedPayloadSize);
            try
            {
                ReadExactly(reader, encryptedPayload);
                var result = new RentedAesGcmData(nonce, encryptedPayload);
                encryptedPayload = null;
                nonce = null;
                return result;
            }
            finally
            {
                encryptedPayload?.Dispose();
            }
        }
        finally
        {
            nonce?.Dispose();
        }
    }

    private void WriteAesGcmData(BinaryWriter writer, RentedAesGcmData data)
    {
        writer.Write(data.Nonce.Data);
        writer.Write(data.EncryptedPayload.Data.Length);
        writer.Write(data.EncryptedPayload.Data, 0, data.EncryptedPayload.Data.Length);
        writer.Flush();
    }
    
    private sealed class RentedAesGcmData : IDisposable
    {
        public readonly IRentedArray<byte> Nonce;

        public readonly IRentedArray<byte> EncryptedPayload;

        public RentedAesGcmData(IRentedArray<byte> nonce, IRentedArray<byte> encryptedPayload)
        {
            Nonce = nonce;
            EncryptedPayload = encryptedPayload;
        }

        public void Dispose()
        {
            Nonce.Dispose();
            EncryptedPayload.Dispose();
        }
    }


}