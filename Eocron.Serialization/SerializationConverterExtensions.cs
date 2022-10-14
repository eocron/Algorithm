using System;
using System.IO;
using System.Text;

namespace Eocron.Serialization
{
    public static class SerializationConverterExtensions
    {
        #region Bytes

        public static byte[] SerializeToBytes<T>(this ISerializationConverter converter, T obj, Encoding encoding = null)
        {
            return SerializeToBytes(converter, typeof(T), obj, encoding);
        }

        public static T Deserialize<T>(this ISerializationConverter converter, byte[] input, Encoding encoding = null)
        {
            return (T)Deserialize(converter, typeof(T), input, encoding);
        }

        public static byte[] SerializeToBytes(this ISerializationConverter converter, Type type, object obj,
            Encoding encoding = null)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            using var stringWriter = new MemoryStream();
            SerializeTo(converter, type, obj, stringWriter);
            return stringWriter.ToArray();
        }

        public static object Deserialize(this ISerializationConverter converter, Type type, byte[] bytes,
            Encoding encoding = null)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            using var byteReader = new MemoryStream(bytes);
            return DeserializeFrom(converter, type, byteReader, encoding);
        }

        #endregion

        #region String

        public static string SerializeToString<T>(this ISerializationConverter converter, T obj, Encoding encoding = null)
        {
            return SerializeToString(converter, typeof(T), obj, encoding);
        }

        public static string SerializeToString(this ISerializationConverter converter, Type type, object obj,
            Encoding encoding = null)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            encoding = encoding ?? SerializationConverter.DefaultEncoding;
            return encoding.GetString(SerializeToBytes(converter, type, obj, encoding));
        }

        public static T Deserialize<T>(this ISerializationConverter converter, string input, Encoding encoding = null)
        {
            return (T)Deserialize(converter, typeof(T), input, encoding);
        }

        public static object Deserialize(this ISerializationConverter converter, Type type, string input,
            Encoding encoding = null)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            encoding = encoding ?? SerializationConverter.DefaultEncoding;
            using var stringReader = StringToStream(input, encoding);
            return DeserializeFrom(converter, type, stringReader, encoding);
        }

        #endregion

        #region StreamReader/StreamWriter

        public static void SerializeTo<T>(this ISerializationConverter converter, T obj, StreamWriter writer)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            converter.SerializeTo(typeof(T), obj, writer);
        }

        public static T DeserializeFrom<T>(this ISerializationConverter converter, StreamReader reader)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            return (T)converter.DeserializeFrom(typeof(T), reader);
        }

        #endregion

        #region Stream

        public static void SerializeTo<T>(this ISerializationConverter converter, T obj, Stream stream,
            Encoding encoding = null)
        {
            SerializeTo(converter, typeof(T), obj, stream, encoding);
        }

        public static T DeserializeFrom<T>(this ISerializationConverter converter, Stream stream,
            Encoding encoding = null)
        {
            return (T)DeserializeFrom(converter, typeof(T), stream, encoding);
        }

        public static object DeserializeFrom(this ISerializationConverter converter, Type type, Stream stream,
            Encoding encoding = null)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            encoding = encoding ?? SerializationConverter.DefaultEncoding;
            var streamReader = new StreamReader(stream, encoding, true, SerializationConverter.DefaultBufferSize, true);
            return converter.DeserializeFrom(type, streamReader);
        }

        public static void SerializeTo(this ISerializationConverter converter, Type type, object obj, Stream stream,
            Encoding encoding = null)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            encoding = encoding ?? SerializationConverter.DefaultEncoding;
            using var streamWriter = new StreamWriter(stream, encoding, SerializationConverter.DefaultBufferSize, true);
            converter.SerializeTo(type, obj, streamWriter);
            streamWriter.Flush();
        }

        #endregion


        private static Stream StringToStream(string input, Encoding encoding)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream, encoding);
            writer.Write(input);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

    }
}