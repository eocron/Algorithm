using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Eocron.Serialization.XmlLegacy;

namespace Eocron.Serialization
{
    public static class SerializationConverter
    {
        public static readonly ISerializationConverter Json = new JsonSerializationConverter();
        public static readonly ISerializationConverter Protobuf = new ProtobufSerializationConverter();
        public static readonly ISerializationConverter Yaml = new YamlSerializationConverter();
        public static readonly ISerializationConverter Xml = new XmlSerializationConverter();
        public static readonly ISerializationConverter XmlDataContract =
            new XmlDocumentSerializationConverter<XmlDocument>(
                new XmlSerializerAdapter<XmlDocument>(x =>
                    new DataContractSerializer(x)));

        public static Encoding DefaultEncoding = new UTF8Encoding(false);
    }
}
