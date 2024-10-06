using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Eocron.Serialization.Tests.Models.XmlLegacy
{
    [XmlRoot("XmlTestModel")]
    public class XmlTestModelFooBar
    {
        public SerializableDictionary<string, string> Dictionary { get; set; }
        [XmlIgnore]
        public TimeSpan TimeSpan { get; set; }

        public long TimeSpanSerializable
        {
            get
            {
                return TimeSpan.Ticks;
            }
            set
            {
                TimeSpan = TimeSpan.FromTicks(value);
            }
        }

        public Guid Guid { get; set; }

        public XmlTestEnum Enum { get; set; }

        [XmlElement(IsNullable = true)]
        public XmlTestModelFooBar Nullable { get; set; }

        public int Integer { get; set; }

        public double Double { get; set; }

        public XmlTestStruct Struct { get; set; }

        public List<int> List { get; set; }

        [XmlArray]
        [XmlArrayItem("int")]
        public long[] Array { get; set; }

        public List<int> EmptyList { get; set; }

        public int[] EmptyArray { get; set; }

        [XmlElement("String")]
        public string FooBarString { get; set; }

        public DateTime DateTime { get; set; }

        public XmlTestModelFooBar NullReference { get; set; }

        public bool Boolean { get; set; }

        public long Long { get; set; }
    }
}