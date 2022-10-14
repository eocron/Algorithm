using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Eocron.Serialization.XmlLegacy
{
    /// <summary>
    /// Adapter for different type of legacy xml document formats (XmlDocument, XDocument)
    /// and different legacy xml serializers (XmlSerializer, DataContractSerializer, XmlObjectSerializer)
    /// </summary>
    /// <typeparam name="TDocument"></typeparam>
    public class XmlSerializerAdapter<TDocument> : IXmlSerializerAdapter<TDocument>
    {
        private readonly Func<Type, XmlObjectSerializer> _xmlObjectSerializerProvider;
        private readonly Func<Type, XmlSerializer> _serializerProvider;

        /// <summary>
        /// Apply only to XmlSerializer
        /// </summary>
        public XmlSerializerNamespaces Namespaces { get; set; }

        /// <summary>
        /// Apply only to XmlObjectSerializer/DataContractSerializer
        /// </summary>
        public ReaderOptions ReaderOptions { get; set; } = ReaderOptions.None;

        /// <summary>
        /// Should adapter verify xml on deserialization. Apply only to XmlObjectSerializer/DataContractSerializer
        /// </summary>
        public bool VerifyObjectName { get; set; } = true;
        public XmlReaderSettings ReaderSettings { get; set; } = new XmlReaderSettings();
        public XmlWriterSettings WriterSettings { get; set; } = new XmlWriterSettings() { Encoding = Encoding.UTF8 };

        /// <summary>
        /// Things such as order of xsd/xsi elements changed. This option fixes it.
        /// </summary>
        public bool EnableCompatibilityWithPreNetCore { get; set; } = true;

        /// <summary>
        /// Adapter for XmlSerializer
        /// </summary>
        /// <param name="serializerProvider"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public XmlSerializerAdapter(Func<Type, XmlSerializer> serializerProvider)
        {
            _serializerProvider = serializerProvider ?? throw new ArgumentNullException(nameof(serializerProvider));
        }

        /// <summary>
        /// Adapter for XmlObjectSerializer/DataContractSerializer
        /// </summary>
        /// <param name="xmlObjectSerializerProvider"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public XmlSerializerAdapter(Func<Type, XmlObjectSerializer> xmlObjectSerializerProvider)
        {
            _xmlObjectSerializerProvider = xmlObjectSerializerProvider ?? throw new ArgumentNullException(nameof(xmlObjectSerializerProvider));
        }

        public TDocument SerializeToDocument(Type type, object content)
        {
            return (TDocument)InternalSerializeToDocument(type, content);
        }

        public object DeserializeFromDocument(Type type, TDocument document)
        {
            return InternalDeserializeFromDocument(type, document);
        }

        public TDocument ReadDocumentFrom(StreamReader sourceStream)
        {
            return (TDocument)InternalReadDocumentFrom(sourceStream);
        }

        public void WriteDocumentTo(StreamWriter targetStream, TDocument document)
        {
            InternalWriteDocumentTo(targetStream, document);
        }

        private XmlObjectSerializer GetXmlObjectSerializer(Type type)
        {
            return _xmlObjectSerializerProvider(type);
        }

        private XmlSerializer GetXmlSerializer(Type type)
        {
            return _serializerProvider(type);
        }

        private object InternalSerializeToDocument(Type type, object content)
        {
            if (_serializerProvider != null)
            {
                if (typeof(TDocument) == typeof(XmlDocument))
                {
                    var document = new XmlDocument();
                    var nav = document.CreateNavigator();
                    using(var w = nav.AppendChild()) 
                        GetXmlSerializer(type).Serialize(w, content, Namespaces);
                    AfterDocumentCreation(document);
                    return document;
                }

                if (typeof(TDocument) == typeof(XDocument))
                {
                    var document = new XDocument();
                    using (var w = document.CreateWriter())
                        GetXmlSerializer(type).Serialize(w, content, Namespaces);
                    AfterDocumentCreation(document);
                    return document;
                }
                throw new NotSupportedException(typeof(TDocument).Name);
            }

            if (typeof(TDocument) == typeof(XmlDocument))
            {
                var document = new XmlDocument();
                var nav = document.CreateNavigator();
                using (var w = nav.AppendChild())
                    GetXmlObjectSerializer(type).WriteObject(w, content);
                AfterDocumentCreation(document);
                return document;
            }

            if (typeof(TDocument) == typeof(XDocument))
            {
                var document = new XDocument();
                using (var w = document.CreateWriter())
                    GetXmlObjectSerializer(type).WriteObject(w, content);
                AfterDocumentCreation(document);
                return document;
            }
            throw new NotSupportedException(typeof(TDocument).Name);
        }

        private object InternalDeserializeFromDocument(Type type, object document)
        {
            if (_serializerProvider != null)
            {
                if (typeof(TDocument) == typeof(XmlDocument))
                {
                    return GetXmlSerializer(type).Deserialize(new XmlNodeReader((XmlDocument)document));
                }

                if (typeof(TDocument) == typeof(XDocument))
                {
                    return GetXmlSerializer(type).Deserialize(((XDocument)document).CreateReader(ReaderOptions));
                }
                throw new NotSupportedException(typeof(TDocument).Name);
            }

            if (typeof(TDocument) == typeof(XmlDocument))
            {
                return GetXmlObjectSerializer(type).ReadObject(new XmlNodeReader((XmlDocument)document), VerifyObjectName);
            }

            if (typeof(TDocument) == typeof(XDocument))
            {
                return GetXmlObjectSerializer(type).ReadObject(((XDocument)document).CreateReader(ReaderOptions), VerifyObjectName);
            }
            throw new NotSupportedException(typeof(TDocument).Name);
        }

        private object InternalReadDocumentFrom(StreamReader sourceStream)
        {
            if (typeof(TDocument) == typeof(XmlDocument))
            {
                using var reader = XmlReader.Create(sourceStream, ReaderSettings);
                var document = new XmlDocument();
                document.Load(reader);
                AfterDocumentCreation(document);
                return document;
            }

            if (typeof(TDocument) == typeof(XDocument))
            {
                using var reader = XmlReader.Create(sourceStream, ReaderSettings);
                var document =  XDocument.Load(reader);
                AfterDocumentCreation(document);
                return document;
            }
            throw new NotSupportedException(typeof(TDocument).Name);
        }

        private void InternalWriteDocumentTo(StreamWriter targetStream, object document)
        {
            if (typeof(TDocument) == typeof(XmlDocument))
            {
                using var xmlTextWriter = XmlWriter.Create(targetStream, WriterSettings);
                ((XmlDocument)document).WriteTo(xmlTextWriter);
                return;
            }

            if (typeof(TDocument) == typeof(XDocument))
            {
                using var xmlTextWriter = XmlWriter.Create(targetStream, WriterSettings);
                ((XDocument)document).WriteTo(xmlTextWriter);
                return;
            }
            throw new NotSupportedException(typeof(TDocument).Name);
        }

        protected virtual void AfterDocumentCreation(XmlDocument doc)
        {
            if (EnableCompatibilityWithPreNetCore)
            {
                ReorderNamespaceAttributes(doc);
            }
        }

        protected virtual void AfterDocumentCreation(XDocument doc)
        {
            if (EnableCompatibilityWithPreNetCore)
            {
                ReorderNamespaceAttributes(doc);
            }
        }
        [Obsolete("For regress only < netcore version")]
        private void ReorderNamespaceAttributes(XmlDocument doc)
        {
            var node = doc.DocumentElement;
            var xsi = node.Attributes["xmlns:xsi"];
            var xsd = node.Attributes["xmlns:xsd"];
            if (xsi != null && xsd != null)
            {
                node.Attributes.InsertAfter(xsd, xsi);
            }
        }

        [Obsolete("For regress only < netcore version")]
        private void ReorderNamespaceAttributes(XDocument doc)
        {
            var node = doc.Root;
            var all = node.Attributes().ToList();

            var xsi = all.FindIndex(x => x.Name.LocalName == "xsi");
            var xsd = all.FindIndex(x => x.Name.LocalName == "xsd");
            if (xsi >= 0 && xsd >= 0 && xsd < xsi)
            {
                (all[xsi], all[xsd]) = (all[xsd], all[xsi]);
                node.ReplaceAttributes(all);
            }
        }

    }
}
