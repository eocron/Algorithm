using System;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.Algorithms.Hex
{
    [Flags]
    public enum HexFormatting : uint
    {
        /// <summary>
        ///     No prefix in hex string or Unix prefix.
        /// </summary>
        Default = None | Unix,

        /// <summary>
        ///     No prefix in hex string.
        /// </summary>
        None = 1 << 0,

        /// <summary>
        ///     Additinal prefix in hex format: 0x
        /// </summary>
        Unix = 1 << 1,

        /// <summary>
        ///     Additinal prefix in hex format: \x
        /// </summary>
        Esc = 1 << 2,

        /// <summary>
        ///     Additinal prefix in hex format: %
        /// </summary>
        Uri = 1 << 3,

        /// <summary>
        ///     Additinal prefix in hex format: &#x
        /// </summary>
        Xml = 1 << 4,

        /// <summary>
        ///     Additinal prefix in hex format: U+
        /// </summary>
        Unicode = 1 << 5,

        /// <summary>
        ///     Additinal prefix in hex format: #
        /// </summary>
        HtmlColor = 1 << 6
    }

    public static class HexEncodingExtensions
    {
        /// <summary>
        ///     Convert HEX representation to byte array.
        /// </summary>
        /// <param name="str">Input string.</param>
        /// <param name="formatting"></param>
        /// <returns></returns>
        public static byte[] FromHexString(this string str, HexFormatting formatting = HexFormatting.Default)
        {
            return InternalConvert(str, formatting, -1, -1);
        }

        /// <summary>
        ///     Convert HEX representation to byte array.
        /// </summary>
        /// <param name="str">Input string.</param>
        /// <param name="offset">Starting position in string.</param>
        /// <param name="count">Count of characters in string.</param>
        /// <param name="formatting"></param>
        /// <returns></returns>
        public static byte[] FromHexString(this string str, int offset, int count,
            HexFormatting formatting = HexFormatting.Default)
        {
            return InternalConvert(str, formatting, offset, count);
        }

        /// <summary>
        ///     Convert byte sequence to HEX representation.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="formatting">HEX string formatting.</param>
        /// <param name="upperCase">Should hex litteral be in upper case.</param>
        /// <returns>Formatted HEX representation of bytes.</returns>
        public static string ToHexString(this byte[] bytes, HexFormatting formatting = HexFormatting.Default,
            bool upperCase = true)
        {
            return ToHexString((ArraySegment<byte>)bytes, formatting, upperCase);
        }

        /// <summary>
        ///     Convert byte sequence to HEX representation.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="formatting">HEX string formatting.</param>
        /// <param name="upperCase">Should hex litteral be in upper case.</param>
        /// <returns>Formatted HEX representation of bytes.</returns>
        public static string ToHexString(this ArraySegment<byte> bytes,
            HexFormatting formatting = HexFormatting.Default, bool upperCase = true)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            var prefix = GetFilteredPrefixes(formatting).FirstOrDefault();
            var offset = prefix?.Length ?? 0;

            var c = new char[offset + bytes.Count * 2];
            for (var i = 0; i < offset; i++)
                c[i] = prefix[i];

            byte b;
            var letterOffset = upperCase ? 0x37 : 0x57;
            const int digitOffset = 0x30;
            for (int bx = 0, cx = 0; bx < bytes.Count; ++bx, ++cx)
            {
                b = (byte)(bytes[bx] >> 4);
                c[offset + cx] = (char)(b > 9 ? b + letterOffset : b + digitOffset);

                b = (byte)(bytes[bx] & 0x0F);
                c[offset + ++cx] = (char)(b > 9 ? b + letterOffset : b + digitOffset);
            }

            return new string(c);
        }

        private static IEnumerable<string> GetFilteredPrefixes(HexFormatting formatting)
        {
            var bitMap = (uint)formatting;
            return PossiblePrefixes.Where((x, i) => (bitMap & (1 << (i + 1))) != 0);
        }

        private static int GetNibble(char c)
        {
            if (c >= '0' && c <= '9')
                return c - '0';
            if (c >= 'a' && c <= 'f')
                return c - 'a' + 10;
            if (c >= 'A' && c <= 'F')
                return c - 'A' + 10;
            throw new ArgumentException("Invalid hex character.");
        }

        private static byte[] InternalConvert(string str, HexFormatting formatting, int offset, int count)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            offset = offset < 0 ? 0 : offset;
            count = count < 0 ? str.Length : count;

            var prefix = GetFilteredPrefixes(formatting).FirstOrDefault(x => str.StartsWith(x));
            if (prefix != null)
            {
                offset += prefix.Length;
                count -= prefix.Length;
            }
            else if (!formatting.HasFlag(HexFormatting.None))
            {
                throw new ArgumentOutOfRangeException(nameof(str), "Invalid hex format.");
            }

            if (count % 2 != 0)
                throw new ArgumentOutOfRangeException(nameof(str), "Invalid hex length.");

            if (count == 0)
                return Array.Empty<byte>();

            var buffer = new byte[count / 2];
            for (int bx = 0, sx = 0; bx < buffer.Length; ++bx, ++sx)
            {
                buffer[bx] = (byte)(GetNibble(str[sx + offset]) << 4);
                buffer[bx] |= (byte)GetNibble(str[++sx + offset]);
            }

            return buffer;
        }

        private static readonly string[] PossiblePrefixes =
        {
            "0x", //unix
            "\\x", //esc
            "%", //uri
            "&#x", //xml
            "U+", //unicode
            "#" //html color
        };
    }
}