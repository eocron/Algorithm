using System;
using System.Collections.Generic;
using System.IO;
using Eocron.Serialization.Tests.Helpers;
using Eocron.Serialization.Tests.Models.Protobuf;
using FluentAssertions;
using NUnit.Framework;

namespace Eocron.Serialization.Tests
{
    [TestFixture]
    public class ProtobufSerializationTests : SerializationTestSuit<ProtobufTestModel>
    {
        public override ISerializationConverter GetConverter()
        {
            return SerializationConverter.Protobuf;
        }

        public override ProtobufTestModel CreateTestModel(string path)
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

        [Test]
        public void CheckSerializeAndDeserializeByBytes()
        {
            AssertSerializeAndDeserializeByBytes(null);
        }

        [Test]
        public void CheckSerializeAndDeserializeByBytesInStream()
        {
            var model = CreateTestModel(null);
            var converter = GetConverter();
            var ms = new MemoryStream();
            converter.SerializeTo(model, ms);
            converter.SerializeTo(model, ms);
            converter.SerializeTo(model, ms);
            ms.Position = 0;

            var deserialized = converter.DeserializeFrom<ProtobufTestModel>(ms);
            deserialized.Should().BeEquivalentTo(model);
            deserialized = converter.DeserializeFrom<ProtobufTestModel>(ms);
            deserialized.Should().BeEquivalentTo(model);
            deserialized = converter.DeserializeFrom<ProtobufTestModel>(ms);
            deserialized.Should().BeEquivalentTo(model);
        }

        [Test]
        [TestCase("TestData/Protobuf/TestModelWithoutBOM.bin")]
        public void CheckDeserializedModelEqualTo(string path)
        {
            AssertDeserializedFromBytesModelEqualTo(path);
        }


        [Test]
        [TestCase("TestData/Protobuf/TestModelWithoutBOM.bin")]
        public void CheckSerializedBytesEqualTo(string path)
        {
            AssertSerializedBytesEqualTo(path, true);
        }
    }
}