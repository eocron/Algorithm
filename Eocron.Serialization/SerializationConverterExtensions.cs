using System;
using System.IO;
using System.Text;

namespace Eocron.Serialization
{
    public static class SerializationConverterExtensions
    {
        public static string SerializeToString<T>(this ISerializationConverter converter, T obj)
        {
            return SerializeToString(converter, typeof(T), obj);
        }

        public static byte[] SerializeToBytes<T>(this ISerializationConverter converter, T obj)
        {
            return SerializeToBytes(converter, typeof(T), obj);
        }

        public static string SerializeToString(this ISerializationConverter converter, Type type, object obj, Encoding encoding = null)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            encoding = encoding ?? GlobalSerializationOptions.Encoding;
            using var stringWriter = new MemoryStream();
            using var streamWriter = new StreamWriter(stringWriter, encoding);
            converter.SerializeToStreamWriter(type, obj, streamWriter);
            streamWriter.Flush();
            return encoding.GetString(stringWriter.ToArray());
        }

        public static byte[] SerializeToBytes(this ISerializationConverter converter, Type type, object obj,
            Encoding encoding = null)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            encoding = encoding ?? GlobalSerializationOptions.Encoding;
            using var stringWriter = new MemoryStream();
            using var streamWriter = new StreamWriter(stringWriter, encoding);
            converter.SerializeToStreamWriter(type, obj, streamWriter);
            streamWriter.Flush();
            return stringWriter.ToArray();
        }

        public static T Deserialize<T>(this ISerializationConverter converter, string input)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            return (T)converter.Deserialize(typeof(T), input);
        }

        public static T Deserialize<T>(this ISerializationConverter converter, byte[] input)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            return (T)converter.Deserialize(typeof(T), input);
        }

        public static T Deserialize<T>(this ISerializationConverter converter, Stream input)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            return (T)converter.Deserialize(typeof(T), input);
        }

        public static object Deserialize(this ISerializationConverter converter, Type type, byte[] bytes,
            Encoding encoding = null)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            using var byteReader = new MemoryStream(bytes);
            return Deserialize(converter, type, byteReader, encoding);
        }

        public static object Deserialize(this ISerializationConverter converter, Type type, string input, Encoding encoding = null)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            encoding = encoding ?? GlobalSerializationOptions.Encoding;
            using var stringReader = StringToStream(input, encoding);
            return Deserialize(converter, type, stringReader, encoding);
        }

        public static object Deserialize(this ISerializationConverter converter, Type type, Stream stream,
            Encoding encoding = null)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            encoding = encoding ?? GlobalSerializationOptions.Encoding;
            var streamReader = new StreamReader(stream, encoding, true, DefaultBufferSize, true);
            return converter.DeserializeFromStreamReader(type, streamReader);
        }

        private static Stream StringToStream(string input, Encoding encoding)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream, encoding);
            writer.Write(input);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private const int DefaultBufferSize = 1024;
    }
}