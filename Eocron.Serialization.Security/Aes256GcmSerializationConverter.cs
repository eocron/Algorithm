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
        using var decryptedPayload = ArrayPoolHelper.Rent(_arrayPool, cipher.GetOutputSize(body.EncryptedPayload.Segment.Count));

        var len = cipher.ProcessBytes(
            body.EncryptedPayload.Segment.Array,
            body.EncryptedPayload.Segment.Offset,
            body.EncryptedPayload.Segment.Count,
            decryptedPayload.Segment.Array,
            decryptedPayload.Segment.Offset);
        cipher.DoFinal(decryptedPayload.Segment.Array, len);
        using var ms = new MemoryStream(decryptedPayload.Segment.Array, decryptedPayload.Segment.Offset, decryptedPayload.Segment.Count, false);
        return _inner.DeserializeFrom(type, ms);
    }

    protected override void SerializeTo(Type type, object obj, BinaryWriter writer)
    {
        var decryptedPayload = new ArraySegment<byte>(_inner.SerializeToBytes(type, obj, Encoding.UTF8));
        using var body = new RentedAesGcmData(
            PasswordDerivationHelper.CreateRandomBytes(NonceByteSize), 
            ArrayPoolHelper.Rent(_arrayPool, decryptedPayload.Count + MacByteSize));
        var cipher = CreateAeadCipher(body.Nonce, true);
        var len = cipher.ProcessBytes(
            decryptedPayload.Array,
            decryptedPayload.Offset,
            decryptedPayload.Count,
            body.EncryptedPayload.Segment.Array,
            body.EncryptedPayload.Segment.Offset);
        cipher.DoFinal(body.EncryptedPayload.Segment.Array, len);
        
        WriteAesGcmData(writer, body);
    }


    
    private IAeadCipher CreateAeadCipher(byte[] nonce, bool forEncryption)
    {
        var cipher = new GcmBlockCipher(new AesLightEngine());
        var parameters = new AeadParameters(new KeyParameter(_key), MacBitSize, nonce);
        cipher.Init(forEncryption, parameters);
        return cipher;
    }

    private void ReadExactly(BinaryReader reader, ArraySegment<byte> segment)
    {
        if (reader.Read(segment.Array, segment.Offset, segment.Count) != segment.Count)
        {
            throw new SecurityException("Integrity check failed. Amount of read bytes doesn't match expected.");
        }
    }

    private RentedAesGcmData ReadAesGcmData(BinaryReader reader)
    {
        var nonce = reader.ReadBytes(NonceByteSize);
        var encryptedPayloadSize = reader.ReadInt32();
        var encryptedPayload = ArrayPoolHelper.Rent(_arrayPool, encryptedPayloadSize);
        try
        {
            ReadExactly(reader, encryptedPayload.Segment);
            var result = new RentedAesGcmData(nonce, encryptedPayload);
            encryptedPayload = null;
            return result;
        }
        finally
        {
            encryptedPayload?.Dispose();
        }
    }

    private void WriteAesGcmData(BinaryWriter writer, RentedAesGcmData data)
    {
        writer.Write(data.Nonce);
        writer.Write(data.EncryptedPayload.Segment.Count);
        writer.Write(data.EncryptedPayload.Segment.Array, data.EncryptedPayload.Segment.Offset, data.EncryptedPayload.Segment.Count);
        writer.Flush();
    }
    
    private sealed class RentedAesGcmData : IDisposable
    {
        public readonly byte[] Nonce;

        public readonly ArrayPoolHelper.RentedByteArray EncryptedPayload;

        public RentedAesGcmData(byte[] nonce, ArrayPoolHelper.RentedByteArray encryptedPayload)
        {
            Nonce = nonce;
            EncryptedPayload = encryptedPayload;
        }

        public void Dispose()
        {
            EncryptedPayload.Dispose();
        }
    }


}