using System;
using System.Collections.Generic;

namespace Eocron.Serialization.Tests.Models.Json
{
    public class JsonTestModel
    {
        public bool Boolean { get; set; }

        public DateTime DateTime { get; set; }
        public Dictionary<string, string> Dictionary { get; set; }

        public double Double { get; set; }

        public Guid Guid { get; set; }

        public int Integer { get; set; }

        public int[] EmptyArray { get; set; }

        public JsonTestEnum Enum { get; set; }

        public JsonTestModel Nullable { get; set; }

        public JsonTestModel NullReference { get; set; }

        public JsonTestStruct Struct { get; set; }

        public List<int> EmptyList { get; set; }

        public List<int> List { get; set; }

        public long Long { get; set; }

        public long[] Array { get; set; }

        public string FooBarString { get; set; }

        public TimeSpan TimeSpan { get; set; }
    }
}