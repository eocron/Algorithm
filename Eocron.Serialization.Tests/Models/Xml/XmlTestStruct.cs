using YAXLib.Attributes;

namespace Eocron.Serialization.Tests.Models.Xml
{
    public struct XmlTestStruct
    {
        [YAXAttributeForClass]
        public int Value { get; set; }
    }
}