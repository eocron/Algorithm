using System;
using System.IO;
using Eocron.Serialization.Xml;

namespace Eocron.Serialization
{
    public sealed class XmlSerializationConverter : ISerializationConverter
    {
        private readonly IXmlDocumentSerializer _serializer;

        public XmlSerializationConverter(IXmlDocumentSerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public object DeserializeFromStreamReader(Type type, StreamReader sourceStream)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));

            return _serializer.DeserializeFromXmlDocument(type, _serializer.ReadFrom(sourceStream));
        }

        public void SerializeToStreamWriter(Type type, object obj, StreamWriter targetStream)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (targetStream == null)
                throw new ArgumentNullException(nameof(targetStream));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            _serializer.WriteTo(targetStream, _serializer.SerializeToXmlDocument(type, obj));
        }
    }
}