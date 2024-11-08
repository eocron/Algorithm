using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Eocron.EncryptedStreams;

public static class Polyval128
{
    public static void Update(ArraySegment<byte> key, ArraySegment<byte> msg, ArraySegment<byte> accumulator)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (msg == null)
            throw new ArgumentNullException(nameof(msg));
        if (accumulator == null)
            throw new ArgumentNullException(nameof(accumulator));
        if (key.Count != PolyvalBlockSize)
            throw new ArgumentOutOfRangeException(nameof(key), $"Invalid key size, should be {PolyvalBlockSize} bytes");
        if (accumulator.Count != PolyvalBlockSize)
            throw new ArgumentOutOfRangeException(nameof(key), $"Invalid accumulator size, should be {PolyvalBlockSize} bytes");
        if (msg.Count % PolyvalBlockSize != 0)
            throw new ArgumentOutOfRangeException(nameof(msg), $"Message size should multiples of {PolyvalBlockSize} bytes");

        var h = new ArraySegment<byte>(new byte[PolyvalBlockSize]);
        var alignedAccumulator = new ArraySegment<byte>(new byte[PolyvalBlockSize]);
        var msgLen = msg.Count;

        var inv128 = new ArraySegment<byte>(new byte[PolyvalBlockSize]);
        Set(inv128, 1UL, 0x9204000000000000UL);

        key.CopyTo(h);
        accumulator.CopyTo(alignedAccumulator);

        Gf2_128_Mul_Polyval(h, inv128);
        while (msgLen > 0)
        {
            Xor(alignedAccumulator, alignedAccumulator, msg);
            Gf2_128_Mul_Polyval(alignedAccumulator, h);
            msg = msg[PolyvalBlockSize..];
            msgLen -= PolyvalBlockSize;
        }

        alignedAccumulator.CopyTo(accumulator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Set(ArraySegment<byte> tgt, ulong lo, ulong hi)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(tgt.Slice(0, 8), lo);
        BinaryPrimitives.WriteUInt64LittleEndian(tgt.Slice(8, 8), hi);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Xor(ArraySegment<byte> result, ArraySegment<byte> a, ArraySegment<byte> b)
    {
        for (var i = 0; i < result.Count; i++)
        {
            result[i] = (byte)(a[i] ^ b[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSet(ArraySegment<byte> data, int bitIndex)
    {
        return (data[bitIndex >> 3] & (1 << (bitIndex % 8))) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Gf2_128_Mul_X_Polyval(ArraySegment<byte> tgt)
    {
        var lo = BinaryPrimitives.ReadUInt64LittleEndian(tgt.Slice(0, 8));
        var hi = BinaryPrimitives.ReadUInt64LittleEndian(tgt.Slice(8, 8));
        var loReducer = (hi & (1UL << 63)) != 0 ? 1UL : 0;
        var hiReducer = (hi & (1UL << 63)) != 0 ? 0xc2UL << 56 : 0;
        Set(tgt, (lo << 1) ^ loReducer, (hi << 1) | (lo >> 63) ^ hiReducer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Gf2_128_Mul_Polyval(ArraySegment<byte> tgt, ArraySegment<byte> src)
    {
        var tmp = new ArraySegment<byte>(new byte[src.Count]);
        for (var i = 0; i < (src.Count << 3); i++)
        {
            if (IsSet(src, i))
            {
                Xor(tmp, tmp, tgt);
            }
            Gf2_128_Mul_X_Polyval(tgt);
        }
        tmp.CopyTo(tgt);
    }

    private const int PolyvalBlockSize = 16;
}