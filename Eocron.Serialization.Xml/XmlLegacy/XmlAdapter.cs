using System;
using System.IO;
using System.Xml;
using Eocron.Serialization.Xml.XmlLegacy.Document;
using Eocron.Serialization.Xml.XmlLegacy.Serializer;

namespace Eocron.Serialization.Xml.XmlLegacy
{
    /// <summary>
    ///     Adapter for different type of legacy xml document formats (XmlDocument, XDocument)
    ///     and different legacy xml serializers (XmlSerializer, DataContractSerializer, XmlObjectSerializer)
    /// </summary>
    /// <typeparam name="TDocument"></typeparam>
    public sealed class XmlAdapter<TDocument> : IXmlAdapter<TDocument>
    {
        public XmlAdapter(IXmlSerializerAdapter serializer, IXmlDocumentAdapter<TDocument> documentAdapter)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _documentAdapter = documentAdapter ?? throw new ArgumentNullException(nameof(documentAdapter));
        }

        public object DeserializeFromDocument(Type type, TDocument document)
        {
            using var reader = _documentAdapter.CreateReader(document);
            return _serializer.ReadObject(reader, type);
        }

        public TDocument ReadDocumentFrom(StreamReader sourceStream)
        {
            using var reader = XmlReader.Create(sourceStream, ReaderSettings);
            var document = _documentAdapter.ReadFrom(reader);
            _documentAdapter.AfterCreation(document);
            return document;
        }

        public TDocument SerializeToDocument(Type type, object content)
        {
            var writer = _documentAdapter.CreateNewDocumentAndWriter(out var document);
            using (var w = writer)
            {
                _serializer.WriteObject(w, type, content);
            }

            _documentAdapter.AfterCreation(document);
            return document;
        }

        public void WriteDocumentTo(StreamWriter targetStream, TDocument document)
        {
            var xmlTextWriter = XmlWriter.Create(targetStream, WriterSettings);
            _documentAdapter.WriteTo(document, xmlTextWriter);
            xmlTextWriter.Flush();
        }

        public XmlReaderSettings ReaderSettings { get; set; } =
            new() { IgnoreComments = true, IgnoreWhitespace = true };

        public XmlWriterSettings WriterSettings { get; set; } = new()
            { Encoding = SerializationConverter.DefaultEncoding, Indent = SerializationConverter.DefaultIndent };

        private readonly IXmlDocumentAdapter<TDocument> _documentAdapter;
        private readonly IXmlSerializerAdapter _serializer;
    }
}