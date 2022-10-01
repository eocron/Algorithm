using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Eocron.Serialization.Xml
{
    public class XmlDocumentSerializer : IXmlDocumentSerializer
    {
        public static XmlWriterSettings DefaultWriterSettings = new XmlWriterSettings()
        {
            Encoding = GlobalSerializationOptions.Encoding
        };
        public static XmlReaderSettings DefaultReaderSettings = new XmlReaderSettings();
        public static Func<Type, XmlSerializer> CreateDefaultSerializer = x => new XmlSerializer(x);

        private readonly XmlWriterSettings _writerSettings;
        private readonly XmlReaderSettings _readerSettings;
        private readonly XmlSerializer _serializer;

        public XmlDocumentSerializer(
            XmlWriterSettings writerSettings = null, 
            XmlReaderSettings readerSettings = null,
            XmlSerializer serializer = null)
        {
            _writerSettings = writerSettings ?? DefaultWriterSettings;
            _readerSettings = readerSettings ?? DefaultReaderSettings;
            _serializer = serializer;
        }

        public object DeserializeFromXmlDocument(Type type, XmlDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            return GetSerializer(type).Deserialize(new XmlNodeReader(document));
        }

        public XmlDocument SerializeToXmlDocument(Type type, object content)
        {
            return SerializeToXmlDocument(GetSerializer(type), content);
        }

        public XmlDocument ReadFrom(StreamReader sourceStream)
        {
            var document = new XmlDocument();
            using var reader = XmlReader.Create(sourceStream, _readerSettings);
            document.Load(reader);
            AfterSerialization(document);
            return document;
        }

        public void WriteTo(XmlDocument document, StreamWriter targetStream)
        {
            using var xmlTextWriter = XmlWriter.Create(targetStream, _writerSettings);
            document.WriteTo(xmlTextWriter);
        }

        private XmlDocument SerializeToXmlDocument(XmlSerializer serializer, object content)
        {
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));
            var document = new XmlDocument();
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, content);
                stream.Position = 0;
                document.Load(XmlReader.Create(stream, _readerSettings));
            }

            AfterSerialization(document);
            return document;
        }

        private XmlSerializer GetSerializer(Type type)
        {
            return _serializer ?? CreateDefaultSerializer?.Invoke(type) ?? throw new ArgumentNullException(nameof(CreateDefaultSerializer));
        }

        protected virtual void AfterSerialization(XmlDocument document)
        {
        }
    }
}