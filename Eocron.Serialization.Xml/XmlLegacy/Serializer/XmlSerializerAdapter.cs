using System;
using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Serialization;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Eocron.Serialization.Xml.XmlLegacy.Serializer
{
    /// <summary>
    ///     Adapting XmlSerializer to common interface
    /// </summary>
    public sealed class XmlSerializerAdapter : IXmlSerializerAdapter
    {
        public XmlSerializerAdapter(Func<Type, XmlSerializer> serializerProvider)
        {
            _serializerProvider = serializerProvider ?? throw new ArgumentNullException(nameof(serializerProvider));
        }

        public object ReadObject(XmlReader reader, Type type)
        {
            return GetXmlSerializer(type).Deserialize(reader);
        }

        public void WriteObject(XmlWriter writer, Type type, object content)
        {
            GetXmlSerializer(type).Serialize(writer, content, Namespaces);
        }

        private XmlSerializer GetXmlSerializer(Type type)
        {
            return _serializerProviderCache.GetOrAdd(type, _serializerProvider);
        }

        public XmlSerializerNamespaces Namespaces { get; set; }
        private readonly ConcurrentDictionary<Type, XmlSerializer> _serializerProviderCache = new();
        private readonly Func<Type, XmlSerializer> _serializerProvider;
    }
}