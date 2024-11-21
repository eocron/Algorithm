using System;
using Eocron.Algorithms.Hex;

namespace Eocron.Algorithms.Caching
{
    internal static class FileCacheShortNameHelper
    {
        public static string GetRandom()
        {
            return ToShortName(Guid.NewGuid().ToByteArray());
        }

        public static string ToShortName(byte[] data)
        {
            return data.ToHexString();
        }
    }
}