using System;
using System.Collections.Generic;
using System.Linq;

namespace Algorithm.Hex
{
    [Flags]
    public enum HexFormatting : uint
    {
        /// <summary>
        /// No prefix in hex string or Unix prefix.
        /// </summary>
        Default = None | Unix,
        /// <summary>
        /// No prefix in hex string.
        /// </summary>
        None = 1 << 0,
        /// <summary>
        /// Additinal prefix in hex format: 0x
        /// </summary>
        Unix = 1 << 1,
        /// <summary>
        /// Additinal prefix in hex format: \x
        /// </summary>
        Esc = 1 << 2,
        /// <summary>
        /// Additinal prefix in hex format: %
        /// </summary>
        Uri = 1 << 3,
        /// <summary>
        /// Additinal prefix in hex format: &#x
        /// </summary>
        Xml = 1 << 4,
        /// <summary>
        /// Additinal prefix in hex format: U+
        /// </summary>
        Unicode = 1 << 5,
        /// <summary>
        /// Additinal prefix in hex format: #
        /// </summary>
        HtmlColor = 1 << 6
    }

    public static class HexEncoding
    {
        private static readonly string[] PossiblePrefixes = new string[]
        {
            "0x",//unix
            "\\x",//esc
            "%",//uri
            "&#x",//xml
            "U+",//unicode
            "#",//html color
        };

        private static IEnumerable<string> GetFilteredPrefixes(HexFormatting formatting)
        {
            var bitMap = (uint)formatting;
            return PossiblePrefixes.Where((x, i) => (bitMap & (1 << (i + 1))) != 0);
        }

        /// <summary>
        /// Convert byte sequence to HEX representation.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="formatting">HEX string formatting.</param>
        /// <param name="upperCase">Should hex litteral be in upper case.</param>
        /// <returns>Formatted HEX representation of bytes.</returns>
        public static string Convert(Span<byte> bytes, HexFormatting formatting = HexFormatting.Default, bool upperCase = false)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            var prefix = GetFilteredPrefixes(formatting).FirstOrDefault();
            var offset = prefix?.Length ?? 0;

            var c = new char[offset + bytes.Length * 2];
            for (int i = 0; i < offset; i++)
                c[i] = prefix[i];

            byte b;
            var letterOffset = upperCase ? 0x17 + 0x20 : 0x37 + 0x20;
            for (int bx = 0, cx = 0; bx < bytes.Length; ++bx, ++cx)
            {
                b = ((byte)(bytes[bx] >> 4));
                c[offset + cx] = (char)(b > 9 ? b + letterOffset : b + 0x30);

                b = ((byte)(bytes[bx] & 0x0F));
                c[offset + (++cx)] = (char)(b > 9 ? b + letterOffset : b + 0x30);
            }

            return new string(c);
        }

        /// <summary>
        /// Convert HEX representation to bytes.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] Convert(string str, HexFormatting formatting = HexFormatting.Default, int offset = -1, int count = -1)
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
            else if(!formatting.HasFlag(HexFormatting.None))
                throw new ArgumentOutOfRangeException(nameof(str), "Invalid hex format.");

            if (count % 2 != 0)
                throw new ArgumentOutOfRangeException(nameof(str), "Invalid hex length.");

            if (count == 0)
                return new byte[0];
            
            var buffer = new byte[count / 2];
            char c;
            for (int bx = 0, sx = 0; bx < buffer.Length; ++bx, ++sx)
            {
                c = str[sx+offset];
                buffer[bx] = (byte)((c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0')) << 4);

                c = str[++sx + offset];
                buffer[bx] |= (byte)(c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0'));
            }

            return buffer;
        }
    }
}
