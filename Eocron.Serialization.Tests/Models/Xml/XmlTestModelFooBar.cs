using System;
using System.Collections.Generic;

namespace Eocron.Serialization.Tests.Models.Xml
{
    public class XmlTestModelFooBar
    {
        public Dictionary<string, string> Dictionary { get; set; }
        public TimeSpan TimeSpan { get; set; }
        public Guid Guid { get; set; }

        public XmlTestEnum Enum { get; set; }
        
        public XmlTestModelFooBar Nullable { get; set; }

        public int Integer { get; set; }

        public double Double { get; set; }

        public XmlTestStruct Struct { get; set; }

        public List<int> List { get; set; }
        
        public long[] Array { get; set; }

        public List<int> EmptyList { get; set; }

        public int[] EmptyArray { get; set; }
        
        public string FooBarString { get; set; }

        public DateTime DateTime { get; set; }

        public XmlTestModelFooBar NullReference { get; set; }

        public bool Boolean { get; set; }

        public long Long { get; set; }
    }
}