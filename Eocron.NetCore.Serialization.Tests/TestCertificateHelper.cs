using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Eocron.NetCore.Serialization.Tests
{
    public static class TestCertificateHelper
    {
        public static X509Certificate2 CreateRsaSelfSignedCertificate()
        {
            var rsa = RSA.Create(2048);
            var req = new CertificateRequest("cn=foobar", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pss);
            var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
            return cert;
        }

        public static X509Certificate2 CreatePublicCertificate(X509Certificate2 privateCert)
        {
            return new X509Certificate2(privateCert.Export(X509ContentType.Cert));
        }
    }
}