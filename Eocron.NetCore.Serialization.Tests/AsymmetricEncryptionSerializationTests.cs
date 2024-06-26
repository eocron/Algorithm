﻿using System;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using Eocron.NetCore.Serialization.Security;
using Eocron.NetCore.Serialization.Tests.Models.Json;
using Eocron.Serialization;
using Eocron.Serialization.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Eocron.NetCore.Serialization.Tests
{
    [TestFixture]
    public class AsymmetricEncryptionSerializationTests : EncryptionSerializationTestsBase
    {
        private X509Certificate2 _privateCertificate;
        private X509Certificate2 _publicCertificate;

        [OneTimeSetUp]
        public void Setup()
        {
            _privateCertificate = TestCertificateHelper.CreateRsaSelfSignedCertificate();
            _publicCertificate = TestCertificateHelper.CreatePublicCertificate(_privateCertificate);
        }

        public override ISerializationConverter GetConverter()
        {
            return GetConverter(_privateCertificate);
        }

        private static ISerializationConverter GetConverter(X509Certificate2 certificate)
        {
            return  new AsymmetricEncryptionSerializationConverter(SerializationConverterJson.Json, certificate);
        }

        [Test]
        public void EncryptWithPublicThenDecryptWithPrivate()
        {
            var publicConverter = GetConverter(_publicCertificate);
            var privateConverter = GetConverter(_privateCertificate);
            
            var model = new JsonTestModel() { Guid = Guid.NewGuid(), FooBarString = "some_string"};
            var data = publicConverter.SerializeToBytes(model);
            var decryptedModel = privateConverter.Deserialize<JsonTestModel>(data);
            decryptedModel.FooBarString.Should().Be("some_string");
            decryptedModel.Should().BeEquivalentTo(model);
        }
        
        [Test]
        public void EncryptWithPublicThenDecryptWithPublicShouldFail()
        {
            var publicConverter = GetConverter(_publicCertificate);
            
            var model = new JsonTestModel() { Guid = Guid.NewGuid(), FooBarString = "some_string"};
            var data = publicConverter.SerializeToBytes(model);
            Assert.Throws<SecurityException>(()=> publicConverter.Deserialize<JsonTestModel>(data));
        }
    }
}