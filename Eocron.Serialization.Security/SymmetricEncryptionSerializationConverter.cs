﻿using System;
using System.IO;
using Eocron.Serialization.Security.Helpers;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Eocron.Serialization.Security;

/// <summary>
/// Symmetric encryption identical to AES256-GCM cipher suit.
/// Used for general encryption where same secret is shared between writers/readers.
/// </summary>
public sealed class SymmetricEncryptionSerializationConverter : BinarySerializationConverterBase
{
    private const int NonceByteSize = 12;
    public const int KeyByteSize = 32;
    private const int MacByteSize = 16;
    private const int MacBitSize = MacByteSize * 8;
    
    private readonly ISerializationConverter _inner;
    private readonly IRentedArrayPool<byte>? _pool;
    private readonly byte[] _key;

    public SymmetricEncryptionSerializationConverter(ISerializationConverter inner, string password, IRentedArrayPool<byte>? pool = null) :
        this(inner, PasswordDerivationHelper.GenerateKeyFrom(password, KeyByteSize), pool)
    {
    }

    public SymmetricEncryptionSerializationConverter(ISerializationConverter inner, byte[] key, IRentedArrayPool<byte>? pool = null)
    {
        if (key == null || key.Length == 0)
            throw new ArgumentNullException(nameof(key));
        if (key.Length != KeyByteSize)
            throw new ArgumentOutOfRangeException(nameof(key), $"Key should be of size: {KeyByteSize}");
        
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _pool = pool ?? NonRentedArrayPool<byte>.Shared;
        _key = key;
    }

    protected override object DeserializeFrom(Type type, BinaryReader reader)
    {
        using var body = ReadAesGcmData(reader);
        var cipher = CreateAeadCipher(body.Nonce, false);
        using var decryptedPayload = _pool.RentExact(cipher.GetOutputSize(body.EncryptedPayload.Data.Length));
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
        using var ms = new MemoryStream();
        _inner.SerializeTo(type, obj, ms);
        using var nonce = PasswordDerivationHelper.CreateRandomBytes(_pool, NonceByteSize);
        using var encrypted = _pool.RentExact((int)ms.Position + MacByteSize);
        using var body = new RentedAesGcmData(nonce, encrypted);
        var cipher = CreateAeadCipher(body.Nonce, true);
        var len = cipher.ProcessBytes(
            ms.GetBuffer(),
            0,
            (int)ms.Position,
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

    private RentedAesGcmData ReadAesGcmData(BinaryReader reader)
    {
        var nonce = _pool.RentExact(NonceByteSize);
        try
        {
            reader.ReadExactly(nonce);
            var encryptedPayloadSize = reader.ReadInt32();
            var encryptedPayload = _pool.RentExact(encryptedPayloadSize);
            try
            {
                reader.ReadExactly(encryptedPayload);
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