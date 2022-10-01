using System;
using System.IO;
using YamlDotNet.Serialization;

namespace Eocron.Serialization
{
    public sealed class YamlSerializationConverter : ISerializationConverter
    {
        private readonly ISerializer _serializer;
        private readonly IDeserializer _deserializer;

        public YamlSerializationConverter(ISerializer serializer, IDeserializer deserializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
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