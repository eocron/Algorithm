using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Eocron.Serialization.Xml.XmlLegacy;
using Eocron.Serialization.Xml.XmlLegacy.Document;
using Eocron.Serialization.Xml.XmlLegacy.Serializer;

namespace Eocron.Serialization.Xml
{
    public static class SerializationConverterXml
    {
        static SerializationConverterXml()
        {
            XmlDocument =
                new XmlSerializationConverter<XmlDocument>(
                    new XmlAdapter<XmlDocument>(
                        new XmlSerializerAdapter(x => new XmlSerializer(x)),
                        new XmlDocumentAdapter()));
            XDocument =
                new XmlSerializationConverter<XDocument>(
                    new XmlAdapter<XDocument>(
                        new XmlSerializerAdapter(x => new XmlSerializer(x)),
                        new XDocumentAdapter()));
            XmlDataContract =
                new XmlSerializationConverter<XmlDocument>(
                    new XmlAdapter<XmlDocument>(
                        new XmlObjectSerializerAdapter(x => new DataContractSerializer(x)),
                        new XmlDocumentAdapter()));
        }
        
        public static readonly ISerializationConverter XmlDocument;
        public static readonly ISerializationConverter XDocument;
        public static readonly ISerializationConverter XmlDataContract;
    }
}