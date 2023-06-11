using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace Eocron.Serialization.XmlLegacy.Xslt
{
    public class XslCompiledTransformAdapter<TDocument> : IXmlAdapter<TDocument>
        where TDocument : new()
    {
        public XslCompiledTransformAdapter(IXmlAdapter<TDocument> inner)
        {
            ValidateDocumentType();
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _xmlDocumentMode = typeof(TDocument) == typeof(XmlDocument);
        }

        public object DeserializeFromDocument(Type type, TDocument document)
        {
            return _inner.DeserializeFromDocument(type, document);
        }

        public TDocument ReadDocumentFrom(StreamReader sourceStream)
        {
            return Transform(_inner.ReadDocumentFrom(sourceStream), OnDeserialize, OnDeserializeArgumentList,
                OnDeserializeReaderOptions);
        }

        public TDocument SerializeToDocument(Type type, object content)
        {
            return Transform(_inner.SerializeToDocument(type, content), OnSerialize, OnSerializeArgumentList,
                OnSerializeReaderOptions);
        }

        public void WriteDocumentTo(StreamWriter targetStream, TDocument document)
        {
            _inner.WriteDocumentTo(targetStream, document);
        }

        protected virtual XmlDocument OnTransform(
            XmlDocument source,
            XslCompiledTransform transform,
            XsltArgumentList arguments,
            ReaderOptions readerOptions)
        {
            var target = new XmlDocument();
            using var writer = target.CreateNavigator().AppendChild();
            using var reader = source.CreateNavigator().ReadSubtree();
            transform.Transform(reader, arguments, writer);
            return target;
        }

        protected virtual XDocument OnTransform(
            XDocument source,
            XslCompiledTransform transform,
            XsltArgumentList arguments,
            ReaderOptions readerOptions)
        {
            var target = new XDocument();

            using var writer = target.CreateWriter();
            using var reader = source.CreateReader(readerOptions);
            transform.Transform(reader, arguments, writer);
            return target;
        }

        private TDocument Transform(
            TDocument sourceDocument,
            XslCompiledTransform transform,
            XsltArgumentList arguments,
            ReaderOptions readerOptions)
        {
            if (transform == null)
                return sourceDocument;

            if (_xmlDocumentMode)
                return (TDocument)(object)OnTransform((XmlDocument)(object)sourceDocument, transform, arguments,
                    readerOptions);

            return (TDocument)(object)OnTransform((XDocument)(object)sourceDocument, transform, arguments,
                readerOptions);
        }

        private void ValidateDocumentType()
        {
            var correct = typeof(TDocument) == typeof(XmlDocument) || typeof(TDocument) == typeof(XDocument);
            if (!correct)
                throw new NotSupportedException(typeof(TDocument).Name);
        }

        public ReaderOptions OnDeserializeReaderOptions { get; set; }
        public ReaderOptions OnSerializeReaderOptions { get; set; }

        public XslCompiledTransform OnDeserialize { get; set; }

        public XslCompiledTransform OnSerialize { get; set; }
        public XsltArgumentList OnDeserializeArgumentList { get; set; }
        public XsltArgumentList OnSerializeArgumentList { get; set; }
        private readonly bool _xmlDocumentMode;
        private readonly IXmlAdapter<TDocument> _inner;
    }
}