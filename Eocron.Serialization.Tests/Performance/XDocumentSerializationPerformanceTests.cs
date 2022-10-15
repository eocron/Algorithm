using System;
using System.Collections.Generic;
using System.Xml.Linq;
using BenchmarkDotNet.Attributes;
using Eocron.Serialization.Tests.Models.XmlLegacy;

namespace Eocron.Serialization.Tests.Performance
{
    public class XDocumentSerializationPerformanceTests : SerializationPerformanceTestsBase<XDocumentSerializationPerformanceTests, XmlTestModelFooBar>
    {
        [Benchmark()]
        public void Deserialize()
        {
            DeserializeText();
        }

        [Benchmark()]
        public void Serialize()
        {
            SerializeText();
        }

        public override ISerializationConverter GetConverter()
        {
            return new XmlSerializationConverter<XDocument>();
        }

        public override XmlTestModelFooBar GetTestModel()
        {
            return new XmlTestModelFooBar
            {
                //Dictionary = new Dictionary<string, string>()
                //{
                //    { "key1", "value1" },
                //    { "key2", "value2" }
                //},
                //TimeSpan = TimeSpan.FromSeconds(3),
                Nullable = null,
                Double = 1.4d,
                Integer = 123,
                List = new List<int> { 1, 2, 3 },
                Array = new long[] { 2, 3, 4 },
                EmptyArray = new int[0],
                EmptyList = new List<int>(),
                FooBarString = "foobar",
                Struct = new XmlTestStruct
                {
                    Value = 234
                },
                DateTime = new DateTime(2022, 1, 1, 1, 1, 1, DateTimeKind.Utc),
                NullReference = null,
                Boolean = true,
                Long = 456,
                Guid = Guid.Parse("1a4c5b27-3881-4330-a13b-f709c004bbc4"),
                Enum = XmlTestEnum.Three
            };
        }
    }
}