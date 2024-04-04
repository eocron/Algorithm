namespace Eocron.Serialization.Protobuf
{
    public static class SerializationConverterProtobuf
    {
        static SerializationConverterProtobuf()
        {
            Protobuf = new ProtobufSerializationConverter();
        }

        public static readonly ISerializationConverter Protobuf;
    }
}