using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace Eocron.Serialization.XmlLegacy
{
    public sealed class XslCompiledTransformAdapter<TDocument> : IXmlSerializerAdapter<TDocument>
        where TDocument : new()
    {
        private readonly IXmlSerializerAdapter<TDocument> _inner;

        public XslCompiledTransform OnSerialize { get; set; }
        public XsltArgumentList OnSerializeArgumentList { get; set; }
        public ReaderOptions OnSerializeReaderOptions { get; set; }

        public XslCompiledTransform OnDeserialize { get; set; }
        public XsltArgumentList OnDeserializeArgumentList { get; set; }
        public ReaderOptions OnDeserializeReaderOptions { get; set; }

        public XslCompiledTransformAdapter(IXmlSerializerAdapter<TDocument> inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public TDocument SerializeToDocument(Type type, object content)
        {
            return Transform(_inner.SerializeToDocument(type, content), OnSerialize, OnSerializeArgumentList, OnSerializeReaderOptions);
        }

        public object DeserializeFromDocument(Type type, TDocument document)
        {
            return _inner.DeserializeFromDocument(type, document);
        }

        public TDocument ReadDocumentFrom(StreamReader sourceStream)
        {
            return Transform(_inner.ReadDocumentFrom(sourceStream), OnDeserialize, OnDeserializeArgumentList, OnDeserializeReaderOptions);
        }

        private static TDocument Transform(
            TDocument sourceDocument, 
            XslCompiledTransform transform,
            XsltArgumentList arguments,
            ReaderOptions readerOptions)
        {
            if (transform == null)
                return sourceDocument;

            if (typeof(TDocument) == typeof(XmlDocument))
            {
                var source = (XmlDocument)(object)sourceDocument;
                var target = new XmlDocument();
                using var writer = target.CreateNavigator().AppendChild();
                using var reader = source.CreateNavigator().ReadSubtree();
                transform.Transform(reader, arguments, writer);
                return (TDocument)(object)target;
            }
            else if (typeof(TDocument) == typeof(XDocument))
            {
                var source = (XDocument)(object)sourceDocument;
                var target = new XDocument();

                using var writer = target.CreateWriter();
                using var reader = source.CreateReader(readerOptions);
                transform.Transform(reader, arguments, writer);
                return (TDocument)(object)target;
            }
            else
            {
                throw new NotSupportedException(typeof(TDocument).Name);
            }
        }

        public void WriteDocumentTo(StreamWriter targetStream, TDocument document)
        {
            _inner.WriteDocumentTo(targetStream, document);
        }
    }
}
