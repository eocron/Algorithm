using System;
using System.IO;
using YamlDotNet.Serialization;

namespace Eocron.Serialization
{
    public sealed class YamlSerializationConverter : ISerializationConverter
    {
        private readonly ISerializer _serializer;
        private readonly IDeserializer _deserializer;

        public static ISerializer DefaultSerializer = new SerializerBuilder().Build();
        public static IDeserializer DefaultDeserializer = new DeserializerBuilder().Build();

        public YamlSerializationConverter(ISerializer serializer = null, IDeserializer deserializer = null)
        {
            _serializer = serializer ?? DefaultSerializer ?? throw new ArgumentNullException(nameof(serializer));
            _deserializer = deserializer ?? DefaultDeserializer ?? throw new ArgumentNullException(nameof(deserializer));
        }

        public object DeserializeFromStreamReader(Type type, StreamReader sourceStream)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));

            return _deserializer.Deserialize(sourceStream, type);
        }

        public void SerializeToStreamWriter(Type type, object obj, StreamWriter targetStream)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (targetStream == null)
                throw new ArgumentNullException(nameof(targetStream));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            _serializer.Serialize(targetStream, obj, type);
        }
    }
}