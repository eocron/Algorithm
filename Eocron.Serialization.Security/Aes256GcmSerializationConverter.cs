using System;
using System.Buffers;
using System.IO;
using System.Security;
using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Eocron.Serialization.Security;

/// <summary>
/// Used for general encryption where secret is not shared between users.
/// AES256 GCM provide two important features: decent security, integrity validation.
/// </summary>
public sealed class Aes256GcmSerializationConverter : ISerializationConverter
{
    private static readonly SecureRandom Random = new SecureRandom();
    private const int NonceByteSize = 12;
    private const int KeyByteSize = 32;
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

    public object DeserializeFrom(Type type, StreamReader sourceStream)
    {
        using var body = ReadAesGcmData(sourceStream);
        var cipher = CreateAeadCipher(body.Nonce, false);
        using var decryptedPayload = Rent(cipher.GetOutputSize(body.EncryptedPayload.Segment.Count));

        var len = cipher.ProcessBytes(
            body.EncryptedPayload.Segment.Array,
            body.EncryptedPayload.Segment.Offset,
            body.EncryptedPayload.Segment.Count,
            decryptedPayload.Segment.Array,
            decryptedPayload.Segment.Offset);
        cipher.DoFinal(decryptedPayload.Segment.Array, len);
        using var ms = new MemoryStream(decryptedPayload.Segment.Array, decryptedPayload.Segment.Offset, decryptedPayload.Segment.Count, false);
        return _inner.DeserializeFrom(type, ms, Encoding.UTF8);
    }
    
    public void SerializeTo(Type type, object obj, StreamWriter targetStream)
    {
        var decryptedPayload = new ArraySegment<byte>(_inner.SerializeToBytes(type, obj, Encoding.UTF8));
        using var body = new RentedAesGcmData(CreateNewNonce(), Rent(decryptedPayload.Count + MacByteSize));
        var cipher = CreateAeadCipher(body.Nonce, true);
        var len = cipher.ProcessBytes(
            decryptedPayload.Array,
            decryptedPayload.Offset,
            decryptedPayload.Count,
            body.EncryptedPayload.Segment.Array,
            body.EncryptedPayload.Segment.Offset);
        cipher.DoFinal(body.EncryptedPayload.Segment.Array, len);
        
        WriteAesGcmData(targetStream, body);
    }

    private RentedByteArray Rent(int size)
    {
        if (size <= 0)
        {
            throw new SecurityException("Invalid rent size.");
        }
        return new RentedByteArray(_arrayPool.Rent(size), size, _arrayPool);
    }
    
    private IAeadCipher CreateAeadCipher(byte[] nonce, bool forEncryption)
    {
        var cipher = new GcmBlockCipher(new AesLightEngine());
        var parameters = new AeadParameters(new KeyParameter(_key), MacBitSize, nonce);
        cipher.Init(forEncryption, parameters);
        return cipher;
    }

    private static byte[] CreateNewNonce()
    {
        var nonce = new byte[NonceByteSize];
        Random.NextBytes(nonce);
        return nonce;
    }
    
    private void ReadExactly(BinaryReader reader, ArraySegment<byte> segment)
    {
        if (reader.Read(segment.Array, segment.Offset, segment.Count) != segment.Count)
        {
            throw new SecurityException("Integrity check failed. Amount of read bytes doesn't match expected.");
        }
    }

    private RentedAesGcmData ReadAesGcmData(StreamReader reader)
    {
        var br = new BinaryReader(reader.BaseStream);
        var nonce = br.ReadBytes(NonceByteSize);
        var encryptedPayloadSize = br.ReadInt32();
        var encryptedPayload = Rent(encryptedPayloadSize);
        try
        {
            ReadExactly(br, encryptedPayload.Segment);
            var result = new RentedAesGcmData(nonce, encryptedPayload);
            encryptedPayload = null;
            return result;
        }
        finally
        {
            encryptedPayload?.Dispose();
        }
    }

    private void WriteAesGcmData(StreamWriter writer, RentedAesGcmData data)
    {
        var bw = new BinaryWriter(writer.BaseStream);
        bw.Write(data.Nonce);
        bw.Write(data.EncryptedPayload.Segment.Count);
        bw.Write(data.EncryptedPayload.Segment.Array, data.EncryptedPayload.Segment.Offset, data.EncryptedPayload.Segment.Count);
        bw.Flush();
    }
    
    private sealed class RentedAesGcmData : IDisposable
    {
        public readonly byte[] Nonce;

        public readonly RentedByteArray EncryptedPayload;

        public RentedAesGcmData(byte[] nonce, RentedByteArray encryptedPayload)
        {
            Nonce = nonce;
            EncryptedPayload = encryptedPayload;
        }

        public void Dispose()
        {
            EncryptedPayload.Dispose();
        }
    }

    private sealed class RentedByteArray : IDisposable
    {
        private readonly ArrayPool<byte> _pool;
        public readonly ArraySegment<byte> Segment;

        public RentedByteArray(byte[] original, int size, ArrayPool<byte> pool)
        {
            _pool = pool;
            Segment = new ArraySegment<byte>(original, 0, size);
        }

        public void Dispose()
        {
            _pool.Return(Segment.Array, true);
        }
    }
}