using System;
using System.IO;
using System.Text;

namespace Eocron.Serialization
{
    public static class SerializationConverterExtensions
    {
        #region Bytes

        public static byte[] SerializeToBytes<T>(this ISerializationConverter converter, T obj)
        {
            return SerializeToBytes(converter, typeof(T), obj);
        }

        public static T Deserialize<T>(this ISerializationConverter converter, byte[] input)
        {
            return (T)Deserialize(converter, typeof(T), input);
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

        #region Base64

        public static string SerializeToBase64String<T>(this ISerializationConverter converter, T obj)
        {
            return SerializeToBase64String(converter, typeof(T), obj);
        }

        public static string SerializeToBase64String(this ISerializationConverter converter, Type type, object obj,
            Encoding encoding = null)
        {
            return Convert.ToBase64String(SerializeToBytes(converter, type, obj, encoding));
        }

        public static T DeserializeFromBase64<T>(this ISerializationConverter converter, string base64Input)
        {
            return Deserialize<T>(converter, Convert.FromBase64String(base64Input));
        }

        public static object DeserializeFromBase64(
            this ISerializationConverter converter,
            Type type,
            string base64Input,
            Encoding encoding = null)
        {
            return Deserialize(converter, type, Convert.FromBase64String(base64Input), encoding);
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

            converter.SerializeToStreamWriter(typeof(T), obj, writer);
        }

        public static T DeserializeFrom<T>(this ISerializationConverter converter, StreamReader reader)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            return (T)converter.DeserializeFromStreamReader(typeof(T), reader);
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
            var streamReader = new StreamReader(stream, encoding, true, DefaultBufferSize, true);
            return converter.DeserializeFromStreamReader(type, streamReader);
        }

        public static void SerializeTo(this ISerializationConverter converter, Type type, object obj, Stream stream,
            Encoding encoding = null)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            encoding = encoding ?? SerializationConverter.DefaultEncoding;
            using var streamWriter = new StreamWriter(stream, encoding, DefaultBufferSize, true);
            converter.SerializeToStreamWriter(type, obj, streamWriter);
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

        private const int DefaultBufferSize = 1024;
    }
}