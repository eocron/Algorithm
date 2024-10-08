using System;
using System.IO;
using Eocron.Serialization.Tests.Models.Json;
using Eocron.Serialization.Tests.Models.Protobuf;
using FluentAssertions;
using NUnit.Framework;

namespace Eocron.Serialization.Tests
{
    public abstract class EncryptionSerializationTestsBase
    {
        public abstract ISerializationConverter GetConverter();

        [Test]
        public void EncryptThenDecrypt()
        {
            var converter = GetConverter();
            var model = new ProtobufTestModel() { Guid = Guid.NewGuid(), FooBarString = "some_string"};
            var data = converter.SerializeToBytes(model);
            var decryptedModel = converter.Deserialize<ProtobufTestModel>(data);
            decryptedModel.FooBarString.Should().Be("some_string");
            decryptedModel.Should().BeEquivalentTo(model);
        }
        
        [Test]
        public void EncryptThenDecryptWithAnotherConverterInstance()
        {
            var converter1 = GetConverter();
            var converter2 = GetConverter();
            var model = new ProtobufTestModel() { Guid = Guid.NewGuid(), FooBarString = "some_string"};
            var data = converter1.SerializeToBytes(model);
            var decryptedModel = converter2.Deserialize<ProtobufTestModel>(data);
            decryptedModel.FooBarString.Should().Be("some_string");
            decryptedModel.Should().BeEquivalentTo(model);
        }
        
        [Test]
        public void EncryptMultipleThenDecrypt()
        {
            var converter = GetConverter();
            var model1 = new ProtobufTestModel() { Guid = Guid.NewGuid(), FooBarString = "some_string"};
            var model2 = new ProtobufTestModel() { Guid = Guid.NewGuid(), FooBarString = "some_string_2"};
            var ms = new MemoryStream();
            converter.SerializeTo(model1, ms);
            converter.SerializeTo(model2, ms);
            ms.Seek(0, SeekOrigin.Begin);
            
            var decryptedModel1 = converter.DeserializeFrom<ProtobufTestModel>(ms);
            decryptedModel1.FooBarString.Should().Be("some_string");
            decryptedModel1.Should().BeEquivalentTo(model1);
            var decryptedModel2 = converter.DeserializeFrom<ProtobufTestModel>(ms);
            decryptedModel2.FooBarString.Should().Be("some_string_2");
            decryptedModel2.Should().BeEquivalentTo(model2);
        }
        
        [Test]
        public void EncryptionDoesNotRepeat()
        {
            var converter = GetConverter();
            var model = new ProtobufTestModel() { Guid = Guid.NewGuid(), FooBarString = "some_string"};
            var data1 = converter.SerializeToBytes(model);
            var data2 = converter.SerializeToBytes(model);
            data1.Should().NotBeEquivalentTo(data2);
            data1.Should().NotBeEmpty();
        }
    }
}