using System;
using System.IO;
using ProtoBuf.Meta;

namespace Eocron.Serialization
{
    public sealed class ProtobufSerializationConverter : ISerializationConverter
    {
        private readonly RuntimeTypeModel _model;

        public ProtobufSerializationConverter(RuntimeTypeModel model = null)
        {
            _model = model ?? RuntimeTypeModel.Default;
        }

        public object DeserializeFromStreamReader(Type type, StreamReader sourceStream)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));

            return _model.Deserialize(type, sourceStream.BaseStream);
        }

        public void SerializeToStreamWriter(Type type, object obj, StreamWriter targetStream)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (targetStream == null)
                throw new ArgumentNullException(nameof(targetStream));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            _model.Serialize(targetStream.BaseStream, obj);
        }
    }
}
