using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Eocron.Serialization.Tests.Models.DataContractXml
{
    [DataContract(Name = "XmlTestModel")]
    public class XmlTestModelFooBar
    {
        [DataMember]
        public Dictionary<string, string> Dictionary { get; set; }
        [DataMember]
        public TimeSpan TimeSpan { get; set; }
        [DataMember]
        public Guid Guid { get; set; }
        [DataMember]
        public XmlTestEnum Enum { get; set; }
        [DataMember(EmitDefaultValue = true)]
        public XmlTestModelFooBar Nullable { get; set; }
        [DataMember]
        public int Integer { get; set; }
        [DataMember]
        public double Double { get; set; }
        [DataMember]
        public XmlTestStruct Struct { get; set; }
        [DataMember]
        public List<int> List { get; set; }
        [DataMember]
        public long[] Array { get; set; }
        [DataMember]
        public List<int> EmptyList { get; set; }
        [DataMember]
        public int[] EmptyArray { get; set; }
        [DataMember]
        public string FooBarString { get; set; }
        [DataMember]
        public DateTime DateTime { get; set; }
        [DataMember]
        public XmlTestModelFooBar NullReference { get; set; }
        [DataMember]
        public bool Boolean { get; set; }
        [DataMember]
        public long Long { get; set; }
    }
}