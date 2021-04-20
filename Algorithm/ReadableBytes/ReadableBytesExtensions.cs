namespace Eocron.Algorithms
{
    public static class ReadableBytesExtensions
    {
        /// <summary>
        /// Return readable file size string.
        /// </summary>
        /// <param name="byteCount"></param>
        /// <param name="format">Default format is: 1.234 MB</param>
        /// <returns></returns>
        public static string ToReadableSizeString(this long byteCount, string format = "{0:F3} {1}")
        {
            var sign = byteCount < 0;
            var absoluteCount = (sign ? -byteCount : byteCount);
            string suffix;
            double readable;
            if (absoluteCount >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = (byteCount >> 50);
            }
            else if (absoluteCount >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (byteCount >> 40);
            }
            else if (absoluteCount >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (byteCount >> 30);
            }
            else if (absoluteCount >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (byteCount >> 20);
            }
            else if (absoluteCount >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (byteCount >> 10);
            }
            else if (absoluteCount >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = byteCount;
            }
            else
            {
                return byteCount.ToString("0 B"); // Byte
            }
            readable = (readable / 1024);
            readable = sign ? -readable : readable;
            return string.Format(format, readable, suffix);
        }
    }
}
