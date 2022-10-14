using System;
using System.IO;
using Eocron.Serialization.XmlLegacy;

namespace Eocron.Serialization
{
    public sealed class XmlDocumentSerializationConverter<TDocument> : ISerializationConverter
    {
        private readonly IXmlSerializerAdapter<TDocument> _serializer;

        public XmlDocumentSerializationConverter(IXmlSerializerAdapter<TDocument> serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public object DeserializeFromStreamReader(Type type, StreamReader sourceStream)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));

            return _serializer.DeserializeFromDocument(type, _serializer.ReadDocumentFrom(sourceStream));
        }

        public void SerializeToStreamWriter(Type type, object obj, StreamWriter targetStream)
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