using System;
using System.IO;
using Newtonsoft.Json;

namespace Eocron.Serialization
{
    public sealed class JsonSerializationConverter : ISerializationConverter
    {
        private readonly JsonSerializer _serializer;

        public JsonSerializationConverter(JsonSerializerSettings settings = null)
        {
            _serializer = JsonSerializer.CreateDefault(settings);
        }

        public object DeserializeFromStreamReader(Type type, StreamReader sourceStream)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));

            using var reader = new JsonTextReader(sourceStream);
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
            _serializer.Serialize(writer, obj, type);
        }
    }
}
