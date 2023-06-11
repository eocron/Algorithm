using System;
using System.Collections.Generic;

namespace Eocron.Serialization.Tests.Models.Yaml
{
    public class YamlTestModel
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

        public long[] Array { get; set; }

        public string FooBarString { get; set; }

        public TimeSpan TimeSpan { get; set; }

        public YamlTestEnum Enum { get; set; }

        public YamlTestModel Nullable { get; set; }

        public YamlTestModel NullReference { get; set; }

        public YamlTestStruct Struct { get; set; }
    }
}