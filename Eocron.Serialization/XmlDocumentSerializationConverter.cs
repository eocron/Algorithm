using System;
using System.IO;
using System.Xml.Serialization;
using Eocron.Serialization.XmlLegacy;

namespace Eocron.Serialization
{
    public sealed class XmlSerializationConverter<TDocument> : ISerializationConverter
    {
        private readonly IXmlSerializerAdapter<TDocument> _serializer;

        public static IXmlSerializerAdapter<TDocument> DefaultXmlSerializerAdapter =
            new XmlSerializerAdapter<TDocument>(x => new XmlSerializer(x));
        public XmlSerializationConverter(IXmlSerializerAdapter<TDocument> serializer = null)
        {
            _serializer = serializer ?? DefaultXmlSerializerAdapter ?? throw new ArgumentNullException(nameof(serializer));
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
    }
}