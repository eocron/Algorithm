namespace Eocron.Serialization.Yaml
{
    public static class SerializationConverterYaml
    {
        static SerializationConverterYaml()
        {
            Yaml = new YamlSerializationConverter();
        }
        public static readonly ISerializationConverter Yaml;
    }
}