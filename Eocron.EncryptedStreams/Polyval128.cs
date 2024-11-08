using System;
using System.Runtime.CompilerServices;

namespace Eocron.EncryptedStreams;

public class Polyval128
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
        BitConverter.TryWriteBytes(inv128.Slice(0, 8), 1UL);
        BitConverter.TryWriteBytes(inv128.Slice(8, 8), 0x9204000000000000UL);

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
    private static void Gf2_128_Mul_X_Polyval(ArraySegment<byte> t)
    {
        var lo = BitConverter.ToUInt64(t.Slice(0, 8));
        var hi = BitConverter.ToUInt64(t.Slice(8, 8));
        var loReducer = (hi & (1UL << 63)) != 0 ? 1UL : 0;
        var hiReducer = (hi & (1UL << 63)) != 0 ? 0xc2UL << 56 : 0;
        BitConverter.TryWriteBytes(t.Slice(8, 8), (hi << 1) | (lo >> 63) ^ hiReducer);
        BitConverter.TryWriteBytes(t.Slice(0, 8), (lo << 1) ^ loReducer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Gf2_128_Mul_Polyval(ArraySegment<byte> r, ArraySegment<byte> b)
    {
        var p = new ArraySegment<byte>(new byte[b.Count]);
        for (var i = 0; i < (b.Count << 3); i++)
        {
            if (IsSet(b, i))
            {
                Xor(p, p, r);
            }
            Gf2_128_Mul_X_Polyval(r);
        }
        p.CopyTo(r);
    }

    private const int PolyvalBlockSize = 16;
}