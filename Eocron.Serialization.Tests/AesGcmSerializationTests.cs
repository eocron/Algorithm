using System;
using System.IO;
using Eocron.Serialization.Security;
using Eocron.Serialization.Tests.Models.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Eocron.Serialization.Tests
{
    [TestFixture]
    public class AesGcmSerializationTests 
    {
        public ISerializationConverter GetConverter()
        {
            return new AesGcmSerializationConverter(SerializationConverter.Json, "foobar");
        }

        [Test]
        public void EncryptThenDecrypt()
        {
            var converter = GetConverter();
            var model = new JsonTestModel() { Guid = Guid.NewGuid(), FooBarString = "some_string"};
            var data = converter.SerializeToBytes(model);
            var decryptedModel = converter.Deserialize<JsonTestModel>(data);
            decryptedModel.FooBarString.Should().Be("some_string");
            decryptedModel.Should().BeEquivalentTo(model);
        }
        
        [Test]
        public void EncryptMultipleThenDecrypt()
        {
            var converter = GetConverter();
            var model1 = new JsonTestModel() { Guid = Guid.NewGuid(), FooBarString = "some_string"};
            var model2 = new JsonTestModel() { Guid = Guid.NewGuid(), FooBarString = "some_string_2"};
            var ms = new MemoryStream();
            converter.SerializeTo(model1, ms);
            converter.SerializeTo(model2, ms);
            ms.Seek(0, SeekOrigin.Begin);
            
            var decryptedModel1 = converter.DeserializeFrom<JsonTestModel>(ms);
            decryptedModel1.FooBarString.Should().Be("some_string");
            decryptedModel1.Should().BeEquivalentTo(model1);
            var decryptedModel2 = converter.DeserializeFrom<JsonTestModel>(ms);
            decryptedModel2.FooBarString.Should().Be("some_string_2");
            decryptedModel2.Should().BeEquivalentTo(model2);
        }
        
        [Test]
        public void EncryptionDoesNotRepeat()
        {
            var converter = GetConverter();
            var model = new JsonTestModel() { Guid = Guid.NewGuid(), FooBarString = "some_string"};
            var data1 = converter.SerializeToBytes(model);
            var data2 = converter.SerializeToBytes(model);
            data1.Should().NotBeEquivalentTo(data2);
            Console.WriteLine(BitConverter.ToString(data1));
            Console.WriteLine(BitConverter.ToString(data2));
        }
    }
}