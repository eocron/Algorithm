using System;
using System.IO;
using Eocron.Serialization.Xml.XmlLegacy;

namespace Eocron.Serialization.Xml
{
    public sealed class XmlSerializationConverter<TDocument> : ISerializationConverter
    {
        public XmlSerializationConverter(IXmlAdapter<TDocument> serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public object DeserializeFrom(Type type, StreamReader sourceStream)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));

            return _serializer.DeserializeFromDocument(type, _serializer.ReadDocumentFrom(sourceStream));
        }

        public void SerializeTo(Type type, object obj, StreamWriter targetStream)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (targetStream == null)
                throw new ArgumentNullException(nameof(targetStream));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            _serializer.WriteDocumentTo(targetStream, _serializer.SerializeToDocument(type, obj));
        }

        private readonly IXmlAdapter<TDocument> _serializer;
    }
}