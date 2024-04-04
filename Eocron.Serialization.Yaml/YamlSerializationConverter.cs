using System;
using System.IO;
using YamlDotNet.Serialization;

namespace Eocron.Serialization.Yaml
{
    public sealed class YamlSerializationConverter : ISerializationConverter
    {
        public object DeserializeFrom(Type type, StreamReader sourceStream)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));

            return Deserializer.Deserialize(sourceStream, type);
        }

        public void SerializeTo(Type type, object obj, StreamWriter targetStream)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (targetStream == null)
                throw new ArgumentNullException(nameof(targetStream));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            Serializer.Serialize(targetStream, obj, type);
        }

        public IDeserializer Deserializer { get; set; } = new DeserializerBuilder().Build();
        public ISerializer Serializer { get; set; } = new SerializerBuilder().Build();
    }
}