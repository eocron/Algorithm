using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Eocron.Serialization.Security;
using NUnit.Framework;

namespace Eocron.Serialization.Tests
{
    [TestFixture]
    public class RsaSerializationTests : SecuredSerializationTests
    {
        private X509Certificate2 _privateCertificate;
        private X509Certificate2 _publicCertificate;

        [OneTimeSetUp]
        public void Setup()
        {
            _privateCertificate = TestCertificateHelper.CreateRsaSelfSignedCertificate();
            _publicCertificate = TestCertificateHelper.CreatePublicCertificate(_privateCertificate);
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            
        }
        public override ISerializationConverter GetConverter()
        {
            var tmp = new RSACryptoServiceProvider();
            tmp.ExportParameters(false);
            return  new RsaSerializationConverter(SerializationConverter.Json, _privateCertificate);
        }
    }
}