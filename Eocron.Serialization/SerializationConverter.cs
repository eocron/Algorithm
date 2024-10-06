using System.Text;

namespace Eocron.Serialization
{
    public static class SerializationConverter
    {
        static SerializationConverter()
        {
            DefaultIndent = true;
            DefaultBufferSize = 1024;
            DefaultEncoding = new UTF8Encoding(false);
        }

        public static readonly bool DefaultIndent;
        public static readonly Encoding DefaultEncoding;
        public static int DefaultBufferSize;
    }
}