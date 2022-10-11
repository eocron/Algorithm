using System;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using Eocron.Serialization.Xml.Transforms;

namespace Eocron.Serialization.Xml
{
    public sealed class ReadableXmlDocumentSerializerWrapper : IXmlDocumentSerializer
    {
        private readonly IXmlDocumentSerializer _inner;
        private readonly Lazy<XslCompiledTransform> _transform;

        public ReadableXmlDocumentSerializerWrapper(IXmlDocumentSerializer inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _transform = new Lazy<XslCompiledTransform>(()=>
            {
                var tmp = new XslCompiledTransform();
                using var reader = XmlReader.Create(new StringReader(XsltResources.ReadableXmlTransform));
                tmp.Load(reader);
                return tmp;
            });
        }

        public object DeserializeFromXmlDocument(Type type, XmlDocument document)
        {
            throw new NotSupportedException();
        }

        public XmlDocument SerializeToXmlDocument(Type type, object obj)
        {
            return Transform(_inner.SerializeToXmlDocument(type, obj));
        }

        public XmlDocument ReadFrom(StreamReader sourceStream)
        {
            throw new NotSupportedException();
        }

        public void WriteTo(StreamWriter targetStream, XmlDocument document)
        {
            _inner.WriteTo(targetStream, document);
        }

        private XmlDocument Transform(XmlDocument source)
        {
            var target = new XmlDocument();
            using var w = target.CreateNavigator().AppendChild();
            using var r = source.CreateNavigator().ReadSubtree();
            _transform.Value.Transform(r, w);
            w.Flush();

            return target;
        }
    }
}
