using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace Eocron.Serialization.Xml
{
    public class XmlObjectDocumentSerializer : IXmlDocumentSerializer
    {
        public static XmlWriterSettings DefaultWriterSettings = new XmlWriterSettings()
        {
            Encoding = GlobalSerializationOptions.Encoding
        };
        public static XmlReaderSettings DefaultReaderSettings = new XmlReaderSettings();

        /// <summary>
        /// DataContractSerializer by default.
        /// </summary>
        public static Func<Type, XmlObjectSerializer> CreateDefaultSerializer = x => new DataContractSerializer(x);

        private readonly XmlWriterSettings _writerSettings;
        private readonly XmlReaderSettings _readerSettings;
        private readonly XmlObjectSerializer _serializer;

        public XmlObjectDocumentSerializer(
            XmlWriterSettings writerSettings = null,
            XmlReaderSettings readerSettings = null,
            XmlObjectSerializer serializer = null)
        {
            _writerSettings = writerSettings ?? DefaultWriterSettings ?? throw new ArgumentNullException(nameof(DefaultWriterSettings));
            _readerSettings = readerSettings ?? DefaultReaderSettings ?? throw new ArgumentNullException(nameof(DefaultReaderSettings));
            _serializer = serializer;
        }

        public object DeserializeFromXmlDocument(Type type, XmlDocument document)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            return GetSerializer(type).ReadObject(new XmlNodeReader(document));
        }

        public XmlDocument SerializeToXmlDocument(Type type, object content)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            var document = new XmlDocument();
            document.AppendChild(document.CreateXmlDeclaration("1.0", null, null));
            var nav = document.CreateNavigator();
            using (var w = nav.AppendChild())
            {
                GetSerializer(type).WriteObject(w, content);
            }
            return document;
        }

        public XmlDocument ReadFrom(StreamReader sourceStream)
        {
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));

            var document = new XmlDocument();
            using var reader = XmlReader.Create(sourceStream, _readerSettings);
            document.Load(reader);
            return document;
        }

        public void WriteTo(StreamWriter targetStream, XmlDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (targetStream == null)
                throw new ArgumentNullException(nameof(targetStream));

            using var xmlTextWriter = XmlWriter.Create(targetStream, _writerSettings);
            document.WriteTo(xmlTextWriter);
        }

        private XmlObjectSerializer GetSerializer(Type type)
        {
            return _serializer ?? CreateDefaultSerializer?.Invoke(type) ?? throw new ArgumentNullException(nameof(CreateDefaultSerializer));
        }
    }
}