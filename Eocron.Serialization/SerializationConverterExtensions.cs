using System;
using System.IO;
using System.Text;
using Eocron.Serialization.Helpers;

namespace Eocron.Serialization
{
    public static class SerializationConverterExtensions
    {
        #region Bytes

        public static byte[] SerializeToBytes<T>(this ISerializationConverter converter, T obj,
            Encoding encoding = null)
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

            using var stream = new MemoryStream();
            SerializeTo(converter, type, obj, stream);
            return stream.ToArray();
        }

        public static object Deserialize(this ISerializationConverter converter, Type type, byte[] bytes,
            Encoding encoding = null)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            using var stream = new MemoryStream(bytes);
            return DeserializeFrom(converter, type, stream, encoding);
        }

        #endregion

        #region String

        public static string SerializeToString<T>(this ISerializationConverter converter, T obj)
        {
            return SerializeToString(converter, typeof(T), obj);
        }

        public static string SerializeToString(this ISerializationConverter converter, Type type, object obj)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            var sb = new StringBuilder();
            converter.SerializeTo(type, obj, new StringStreamWriter(sb));
            return sb.ToString();
        }

        public static T Deserialize<T>(this ISerializationConverter converter, string input)
        {
            return (T)Deserialize(converter, typeof(T), input);
        }

        public static object Deserialize(this ISerializationConverter converter, Type type, string input)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            return converter.DeserializeFrom(type, new StringStreamReader(input));
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
            using var streamReader =
                new StreamReader(stream, encoding, true, SerializationConverter.DefaultBufferSize, true);
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
    }
}