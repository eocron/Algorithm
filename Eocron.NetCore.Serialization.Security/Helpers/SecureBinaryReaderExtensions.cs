using System.IO;
using System.Security;

namespace Eocron.NetCore.Serialization.Security.Helpers
{
    internal static class SecureBinaryReaderExtensions
    {
        public static void ReadExactly(this BinaryReader reader, IRentedArray<byte> segment)
        {
            var read = reader.Read(segment.Data);
            if (read != segment.Data.Length)
            {
                throw new SecurityException("Integrity check failed. Amount of read bytes doesn't match expected.");
            }
        }
    }
}