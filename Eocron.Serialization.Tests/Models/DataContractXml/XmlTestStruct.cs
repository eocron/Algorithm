using System.Runtime.Serialization;

namespace Eocron.Serialization.Tests.Models.DataContractXml
{
    [DataContract]
    public class XmlTestStruct
    {
        [DataMember]
        public int Value;
    }
}