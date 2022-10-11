using System.Runtime.Serialization;

namespace Eocron.Serialization.Tests.Models.DataContractXml
{
    [DataContract]
    public enum XmlTestEnum
    {
        [EnumMember]
        One,
        [EnumMember]
        Two,
        [EnumMember]
        Three,
    }
}