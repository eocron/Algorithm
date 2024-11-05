using System;

namespace Eocron.Algorithms.HashCode;

public static class HashBytesExtensions
{
    public static Guid ToGuid(this HashBytes hashBytes)
    {
        var result = new byte[16];
        Array.Copy(hashBytes.Value, 0, result, 0, Math.Min(hashBytes.Value.Length, result.Length));
        return new Guid(CreateReSized(hashBytes.Value, 16));
    }

    private static byte[] CreateReSized(byte[] data, int targetSize)
    {
        var result = new byte[targetSize];
        Array.Copy(data, 0, result, 0, Math.Min(data.Length, result.Length));
        return result;
    }
}