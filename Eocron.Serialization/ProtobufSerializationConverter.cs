using System;
using System.IO;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Eocron.Serialization
{
    public sealed class ProtobufSerializationConverter : ISerializationConverter
    {
        public ProtobufSerializationConverter(
            RuntimeTypeModel model = null,
            bool addLengthPrefix = true,
            PrefixStyle prefixStyle = PrefixStyle.Fixed32,
            int fieldNumber = -1)
        {
            _addLengthPrefix = addLengthPrefix;
            _prefixStyle = prefixStyle;
            _fieldNumber = fieldNumber;
            _model = model ?? DefaultRuntimeTypeModel ?? throw new ArgumentNullException(nameof(model));
        }

        public object DeserializeFrom(Type type, StreamReader sourceStream)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (sourceStream.BaseStream == null)
                throw new ArgumentNullException(nameof(sourceStream.BaseStream));

            if (_addLengthPrefix)
                return _model.DeserializeWithLengthPrefix(sourceStream.BaseStream, null, type, _prefixStyle,
                    _fieldNumber);
            return _model.Deserialize(type, sourceStream.BaseStream);
        }

        public void SerializeTo(Type type, object obj, StreamWriter targetStream)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (targetStream == null)
                throw new ArgumentNullException(nameof(targetStream));
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (targetStream.BaseStream == null)
                throw new ArgumentNullException(nameof(targetStream.BaseStream));

            if (_addLengthPrefix)
                _model.SerializeWithLengthPrefix(targetStream.BaseStream, obj, type, _prefixStyle, -1);
            else
                _model.Serialize(targetStream.BaseStream, obj);
        }

        public static RuntimeTypeModel DefaultRuntimeTypeModel = RuntimeTypeModel.Default;
        private readonly bool _addLengthPrefix;
        private readonly int _fieldNumber;
        private readonly PrefixStyle _prefixStyle;
        private readonly RuntimeTypeModel _model;
    }
}