namespace Eocron.Serialization.Json
{
    public static class SerializationConverterJson
    {
        static SerializationConverterJson()
        {
            Json = new JsonSerializationConverter();
        }
        public static readonly ISerializationConverter Json;
    }
}