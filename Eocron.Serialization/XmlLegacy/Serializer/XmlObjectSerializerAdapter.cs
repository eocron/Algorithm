using System;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Xml;

namespace Eocron.Serialization.XmlLegacy.Serializer
{
    /// <summary>
    ///     Adapting XmlObjectSerializer/DataContractSerializer to common interface
    /// </summary>
    public sealed class XmlObjectSerializerAdapter : IXmlSerializerAdapter
    {
        public XmlObjectSerializerAdapter(Func<Type, XmlObjectSerializer> xmlObjectSerializerProvider)
        {
            _xmlObjectSerializerProvider = xmlObjectSerializerProvider ??
                                           throw new ArgumentNullException(nameof(xmlObjectSerializerProvider));
        }

        public object ReadObject(XmlReader reader, Type type)
        {
            return GetXmlObjectSerializer(type).ReadObject(reader, VerifyObjectName);
        }

        public void WriteObject(XmlWriter writer, Type type, object content)
        {
            GetXmlObjectSerializer(type).WriteObject(writer, content);
        }

        private XmlObjectSerializer GetXmlObjectSerializer(Type type)
        {
            return _xmlObjectSerializerProviderCache.GetOrAdd(type, _xmlObjectSerializerProvider);
        }

        public bool VerifyObjectName { get; set; }
        private readonly ConcurrentDictionary<Type, XmlObjectSerializer> _xmlObjectSerializerProviderCache = new();
        private readonly Func<Type, XmlObjectSerializer> _xmlObjectSerializerProvider;
    }
}