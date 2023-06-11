using System;
using System.IO;
using Newtonsoft.Json;

namespace Eocron.Serialization
{
    public sealed class JsonSerializationConverter : ISerializationConverter
    {
        public object DeserializeFrom(Type type, StreamReader sourceStream)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));

            using var reader = new JsonTextReader(sourceStream);
            reader.CloseInput = false;
            return Serializer.Deserialize(reader, type);
        }

        public void SerializeTo(Type type, object obj, StreamWriter targetStream)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (targetStream == null)
                throw new ArgumentNullException(nameof(targetStream));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            using var writer = new JsonTextWriter(targetStream);
            writer.CloseOutput = false;
            Serializer.Serialize(writer, obj, type);
            writer.Flush();
        }

        public JsonSerializer Serializer { get; set; } = JsonSerializer.CreateDefault(new JsonSerializerSettings
        {
            Formatting = SerializationConverter.DefaultIndent ? Formatting.Indented : Formatting.None
        });
    }
}