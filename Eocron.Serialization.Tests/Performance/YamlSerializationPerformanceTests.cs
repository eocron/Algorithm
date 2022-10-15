using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Eocron.Serialization.Tests.Models.Yaml;

namespace Eocron.Serialization.Tests.Performance
{
    public class YamlSerializationPerformanceTests : SerializationPerformanceTestsBase<YamlSerializationPerformanceTests, YamlTestModel>
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
            return SerializationConverter.Yaml;
        }

        public override YamlTestModel GetTestModel()
        {
            return new YamlTestModel
            {
                Dictionary = new Dictionary<string, string>
                {
                    { "key1", "value1" },
                    { "key2", "value2" }
                },
                TimeSpan = TimeSpan.FromSeconds(3),
                Nullable = null,
                Double = 1.4d,
                Integer = 123,
                List = new List<int> { 1, 2, 3 },
                Array = new long[] { 2, 3, 4 },
                EmptyArray = new int[0],
                EmptyList = new List<int>(),
                FooBarString = "foobar",
                Struct = new YamlTestStruct()
                {
                    Value = 234
                },
                DateTime = new DateTime(2022, 1, 1, 1, 1, 1, DateTimeKind.Utc),
                NullReference = null,
                Boolean = true,
                Long = 456,
                Guid = Guid.Parse("1a4c5b27-3881-4330-a13b-f709c004bbc4"),
                Enum = YamlTestEnum.Three
            };
        }
    }
}