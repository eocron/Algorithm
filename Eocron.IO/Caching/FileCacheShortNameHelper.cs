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
            var sb = new StringBuilder();
            var bigInt = new BigInteger(data);
            while (bigInt > 0)
            {
                sb.Append(Mask[(int)(bigInt % Mask.Length)]);
                bigInt /= Mask.Length;
            }
            return sb.ToString();
        }

        private static readonly char[] Mask = "abcdefghijklmnopqrstuvwxyz0123456789_-".ToCharArray();
    }
}