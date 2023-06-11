using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Eocron.Serialization.Tests.Models.XmlLegacy
{
    [XmlRoot("XmlTestModel")]
    public class XmlTestModelFooBar
    {
        public bool Boolean { get; set; }

        public DateTime DateTime { get; set; }

        public double Double { get; set; }

        public Guid Guid { get; set; }

        public int Integer { get; set; }

        public int[] EmptyArray { get; set; }

        public List<int> EmptyList { get; set; }

        public List<int> List { get; set; }

        public long Long { get; set; }

        public long TimeSpanSerializable
        {
            get => TimeSpan.Ticks;
            set => TimeSpan = TimeSpan.FromTicks(value);
        }

        [XmlArray] [XmlArrayItem("int")] public long[] Array { get; set; }

        public SerializableDictionary<string, string> Dictionary { get; set; }

        [XmlElement(IsNullable = true)] public XmlTestModelFooBar Nullable { get; set; }

        [XmlElement("String")] public string FooBarString { get; set; }

        [XmlIgnore] public TimeSpan TimeSpan { get; set; }

        public XmlTestEnum Enum { get; set; }


        public XmlTestModelFooBar NullReference { get; set; }

        public XmlTestStruct Struct { get; set; }
    }
}