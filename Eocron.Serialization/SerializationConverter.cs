using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Eocron.Serialization.XmlLegacy;
using Eocron.Serialization.XmlLegacy.Document;
using Eocron.Serialization.XmlLegacy.Serializer;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace Eocron.Serialization
{
    public static class SerializationConverter
    {
        public static readonly ISerializationConverter Json = new JsonSerializationConverter(
            new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented
            });
        public static readonly ISerializationConverter Protobuf = new ProtobufSerializationConverter();
        public static readonly ISerializationConverter Yaml = new YamlSerializationConverter();

        public static readonly ISerializationConverter XmlDocument =
            new XmlSerializationConverter<XmlDocument>(
                new XmlAdapter<XmlDocument>(
                    new XmlSerializerAdapter(x => new XmlSerializer(x)),
                    new XmlDocumentAdapter()));
        public static readonly ISerializationConverter XDocument =
            new XmlSerializationConverter<XDocument>(
                new XmlAdapter<XDocument>(
                    new XmlSerializerAdapter(x => new XmlSerializer(x)),
                    new XDocumentAdapter()));
        public static readonly ISerializationConverter XmlDataContract =
            new XmlSerializationConverter<XmlDocument>(
                new XmlAdapter<XmlDocument>(
                    new XmlObjectSerializerAdapter(x => new DataContractSerializer(x)),
                    new XmlDocumentAdapter()));

        public static Encoding DefaultEncoding = new UTF8Encoding(false);
        public static int DefaultBufferSize = 1024;
    }
}
