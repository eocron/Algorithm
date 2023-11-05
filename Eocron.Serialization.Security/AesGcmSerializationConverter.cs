using System;
using System.Buffers;
using System.IO;
using System.Security;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Eocron.Serialization.Security;

/// <summary>
/// Used when Reader and Writer are the same person. Use RSA version when Writers are public, and Readers are private.
/// </summary>
public sealed class AesGcmSerializationConverter : ISerializationConverter
{
    private readonly ISerializationConverter _inner;
    private static readonly SecureRandom Random = new SecureRandom();
    private readonly ArrayPool<byte> _arrayPool;
    private readonly PasswordDerivative _passwordDerivative;

    private const int MacByteSize = 12;
    private const int NonceByteSize = 16;
    private const int SaltByteSize = 16;
    private const int KeyByteSize = 32;

    private const int MacBitSize = MacByteSize * 8;

    public AesGcmSerializationConverter(ISerializationConverter inner, string password)
    {
        _inner = inner;
        _arrayPool = ArrayPool<byte>.Shared;
        _passwordDerivative = PasswordDerivationHelper.GenerateFrom(password, SaltByteSize, KeyByteSize);
    }

    public object DeserializeFrom(Type type, StreamReader sourceStream)
    {
        var br = new BinaryReader(sourceStream.BaseStream);
        var totalSize = br.ReadInt32();
        using var all = Rent(totalSize);
        ReadExactly(br, all.Segment);
        var nonceSegment = new ArraySegment<byte>(all.Segment.Array, 0, NonceByteSize);
        var payloadSegment = new ArraySegment<byte>(all.Segment.Array, NonceByteSize, totalSize - NonceByteSize);
        var cipher = CreateAeadCipher(ToArray(nonceSegment), false);
        using var decrypted = Rent(cipher.GetOutputSize(payloadSegment.Count));

        var len = cipher.ProcessBytes(
            payloadSegment.Array,
            payloadSegment.Offset,
            payloadSegment.Count,
            decrypted.Segment.Array,
            decrypted.Segment.Offset);
        cipher.DoFinal(decrypted.Segment.Array, len);

        using var ms = new MemoryStream(decrypted.Segment.Array, decrypted.Segment.Offset, decrypted.Segment.Count,
            false);
        using var sr = new StreamReader(ms);
        return _inner.DeserializeFrom(type, sr);
    }

    private IAeadCipher CreateAeadCipher(byte[] nonce, bool forEncryption)
    {
        var cipher = new GcmBlockCipher(new AesLightEngine());
        var parameters = new AeadParameters(new KeyParameter(_passwordDerivative.Hash), MacBitSize, nonce);
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

    private byte[] ToArray(ArraySegment<byte> segment)
    {
        var res = new byte[segment.Count];
        Array.Copy(segment.Array, segment.Offset, res, 0, segment.Count);
        return res;
    }

    public void SerializeTo(Type type, object obj, StreamWriter targetStream)
    {
        using var ms = new MemoryStream();
        using var sw = new StreamWriter(ms);
        _inner.SerializeTo(type, obj, sw);
        sw.Flush();
        var payloadSegment = new ArraySegment<byte>(ms.ToArray(), 0, (int)ms.Length);
        var nonce = new byte[NonceByteSize];
        Random.NextBytes(nonce);
        var cipher = CreateAeadCipher(nonce, true);
        using var encryptedPayload = Rent(cipher.GetOutputSize(payloadSegment.Count));
        var len = cipher.ProcessBytes(
            payloadSegment.Array,
            payloadSegment.Offset,
            payloadSegment.Count,
            encryptedPayload.Segment.Array,
            encryptedPayload.Segment.Offset);
        cipher.DoFinal(encryptedPayload.Segment.Array, len);

        var bw = new BinaryWriter(targetStream.BaseStream);
        bw.Write(nonce.Length + encryptedPayload.Segment.Count);
        bw.Write(nonce);
        bw.Write(encryptedPayload.Segment.Array, encryptedPayload.Segment.Offset, encryptedPayload.Segment.Count);
        bw.Flush();
    }

    private RentedByteArray Rent(int size)
    {
        if (size <= 0)
        {
            throw new SecurityException("Invalid rent size.");
        }
        return new RentedByteArray(_arrayPool.Rent(size), size, _arrayPool);
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
            _pool.Return(Segment.Array);
        }
    }
}