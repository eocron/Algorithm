using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Eocron.Serialization.Tests.Models.DataContract
{
    [XmlRoot("XmlTestModel")]
    public class XmlTestModelFooBar
    {
        public bool Boolean { get; set; }

        public DateTime DateTime { get; set; }
        public Dictionary<string, string> Dictionary { get; set; }

        public double Double { get; set; }
        public Guid Guid { get; set; }

        public int Integer { get; set; }

        public int[] EmptyArray { get; set; }

        public List<int> EmptyList { get; set; }

        public List<int> List { get; set; }

        public long Long { get; set; }

        [XmlArray] [XmlArrayItem("int")] public long[] Array { get; set; }

        [XmlElement(IsNullable = true)] public XmlTestModelFooBar Nullable { get; set; }

        [XmlElement("String")] public string FooBarString { get; set; }

        public TimeSpan TimeSpan { get; set; }

        public XmlTestEnum Enum { get; set; }


        public XmlTestModelFooBar NullReference { get; set; }

        public XmlTestStruct Struct { get; set; }
    }
}