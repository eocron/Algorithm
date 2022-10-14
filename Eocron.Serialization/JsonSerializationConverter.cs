using System;
using System.IO;
using Newtonsoft.Json;

namespace Eocron.Serialization
{
    public sealed class JsonSerializationConverter : ISerializationConverter
    {
        private readonly JsonSerializer _serializer;

        public static JsonSerializerSettings DefaultJsonSerializerSettings =
            new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };

        public JsonSerializationConverter(JsonSerializerSettings settings = null)
        {
            _serializer = JsonSerializer.CreateDefault(settings ??
                                                       DefaultJsonSerializerSettings ??
                                                       throw new ArgumentNullException(nameof(settings)));
        }

        public object DeserializeFromStreamReader(Type type, StreamReader sourceStream)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));

            using var reader = new JsonTextReader(sourceStream);
            reader.CloseInput = false;
            return _serializer.Deserialize(reader, type);
        }

        public void SerializeToStreamWriter(Type type, object obj, StreamWriter targetStream)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (targetStream == null)
                throw new ArgumentNullException(nameof(targetStream));
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            
            using var writer = new JsonTextWriter(targetStream);
            writer.CloseOutput = false;
            _serializer.Serialize(writer, obj, type);
        }
    }
}
