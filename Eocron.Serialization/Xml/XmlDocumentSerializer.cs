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
        private readonly XmlSerializerNamespaces _namespaces;
        private readonly bool _enableBackwardCompatibility;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writerSettings"></param>
        /// <param name="readerSettings"></param>
        /// <param name="serializer"></param>
        /// <param name="namespaces"></param>
        /// <param name="enableBackwardCompatibility">Output XML same as in net471. Default: true</param>
        public XmlDocumentSerializer(
            XmlWriterSettings writerSettings = null, 
            XmlReaderSettings readerSettings = null,
            XmlSerializer serializer = null,
            XmlSerializerNamespaces namespaces = null,
            bool enableBackwardCompatibility = true)
        {
            _writerSettings = writerSettings ?? DefaultWriterSettings;
            _readerSettings = readerSettings ?? DefaultReaderSettings;
            _serializer = serializer;
            _namespaces = namespaces;
            _enableBackwardCompatibility = enableBackwardCompatibility;
        }

        public object DeserializeFromXmlDocument(Type type, XmlDocument document)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            return GetSerializer(type).Deserialize(new XmlNodeReader(document));
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
                GetSerializer(type).Serialize(w, content, _namespaces);
            }
            AfterSerialization(document);
            return document;
        }

        public XmlDocument ReadFrom(StreamReader sourceStream)
        {
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));

            var document = new XmlDocument();
            using var reader = XmlReader.Create(sourceStream, _readerSettings);
            document.Load(reader);
            AfterSerialization(document);
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

        private XmlSerializer GetSerializer(Type type)
        {
            return _serializer ?? CreateDefaultSerializer?.Invoke(type) ?? throw new ArgumentNullException(nameof(CreateDefaultSerializer));
        }

        [Obsolete("For regress only (net6.0)")]
        private static void StripEncodingAttribute(XmlDocument document)
        {
            var declaration = document.FirstChild as XmlDeclaration;
            if (declaration == null)
                return;

            declaration.Encoding = null;
        }

        [Obsolete("For regress only (net6.0, net5.0, netcore3.1)")]
        private static void ReorderNamespaceAttributes(XmlElement node)
        {
            var xsi = node.Attributes["xmlns:xsi"];
            var xsd = node.Attributes["xmlns:xsd"];
            if (xsi != null && xsd != null)
            {
                node.Attributes.InsertAfter(xsi, xsd);
            }
        }

        private void AfterSerialization(XmlDocument document)
        {
            if (_enableBackwardCompatibility)
            {
                ReorderNamespaceAttributes(document.DocumentElement);
                StripEncodingAttribute(document);
            }
        }
    }
}