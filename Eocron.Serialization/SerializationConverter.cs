using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Eocron.Serialization.XmlLegacy;
using Eocron.Serialization.XmlLegacy.Document;
using Eocron.Serialization.XmlLegacy.Serializer;

namespace Eocron.Serialization
{
    public static class SerializationConverter
    {
        static SerializationConverter()
        {
            DefaultIndent = true;
            DefaultBufferSize = 1024;
            DefaultEncoding = new UTF8Encoding(false);

            Json = new JsonSerializationConverter();
            Protobuf = new ProtobufSerializationConverter();
            Yaml = new YamlSerializationConverter();

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

        public static readonly ISerializationConverter Json;
        public static readonly ISerializationConverter Protobuf;
        public static readonly ISerializationConverter Yaml;

        public static readonly ISerializationConverter XmlDocument;
        public static readonly ISerializationConverter XDocument;
        public static readonly ISerializationConverter XmlDataContract;

        public static bool DefaultIndent;
        public static Encoding DefaultEncoding;
        public static int DefaultBufferSize;
    }
}
