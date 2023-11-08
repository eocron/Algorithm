using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Eocron.Serialization.Tests.Models.Protobuf;

namespace Eocron.Serialization.PerformanceTests
{
    public class ProtobufSerializationPerformanceTests :
        SerializationPerformanceTestsBase<ProtobufSerializationPerformanceTests, ProtobufTestModel>,
        ISerializationPerformanceTests
    {
        public ProtobufSerializationPerformanceTests() : base(false)
        {
        }

        [Benchmark]
        public void Deserialize()
        {
            DeserializeBinary();
        }

        public override ISerializationConverter GetConverter()
        {
            return SerializationConverter.Protobuf;
        }

        public override ProtobufTestModel GetTestModel()
        {
            return new ProtobufTestModel
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
                EmptyArray = null,
                EmptyList = null,
                FooBarString = "foobar",
                Struct = new ProtobufTestStruct
                {
                    Value = 234
                },
                DateTime = new DateTime(2022, 1, 1, 1, 1, 1, DateTimeKind.Utc),
                NullReference = null,
                Boolean = true,
                Long = 456,
                Guid = Guid.Parse("1a4c5b27-3881-4330-a13b-f709c004bbc4"),
                Enum = ProtobufTestEnum.Three
            };
        }

        [Benchmark]
        public void Serialize()
        {
            SerializeBinary();
        }
    }
}