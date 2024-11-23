using System;
using System.Numerics;
using System.Text;

namespace Eocron.IO.Caching
{
    internal static class FileCacheShortNameHelper
    {
        public static string GetRandom()
        {
            return ToShortName(Guid.NewGuid().ToByteArray());
        }

        public static string ToShortName(byte[] data)
        {
            return Convert.ToBase64String(data);
        }
    }
}