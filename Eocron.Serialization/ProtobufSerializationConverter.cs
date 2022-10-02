using System;
using System.IO;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Eocron.Serialization
{
    public sealed class ProtobufSerializationConverter : ISerializationConverter
    {
        private readonly bool _addLengthPrefix;
        private readonly RuntimeTypeModel _model;

        public ProtobufSerializationConverter(RuntimeTypeModel model = null, bool addLengthPrefix = true)
        {
            _addLengthPrefix = addLengthPrefix;
            _model = model ?? RuntimeTypeModel.Default;
        }

        public object DeserializeFromStreamReader(Type type, StreamReader sourceStream)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));

            if (_addLengthPrefix)
            {
                return _model.DeserializeWithLengthPrefix(sourceStream.BaseStream, null, type, PrefixStyle.Fixed32, -1);
            }
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

            if (_addLengthPrefix)
            {
                _model.SerializeWithLengthPrefix(targetStream.BaseStream, obj, type, PrefixStyle.Fixed32, -1);
            }
            else
            {
                _model.Serialize(targetStream.BaseStream, obj);
            }
        }
    }
}
