using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Eocron.Algorithms.Certificates
{
    public static class CertificateHelper
    {
        public static X509Certificate2 FindByThumbprint(string thumbprint)
        {
            if (string.IsNullOrWhiteSpace(thumbprint))
                throw new ArgumentException("Thumbprint cannot be null or whitespace.", nameof(thumbprint));
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            return store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false).Single();
        }

        public static string ExportCertificateToPem(X509Certificate2 cert)
        {
            return cert.ExportCertificatePem();
        }

        public static string ExportPublicKeyToPem(X509Certificate2 cert)
        {
            using var rsa = (AsymmetricAlgorithm)cert.GetRSAPublicKey() ?? cert.GetECDsaPublicKey();
            return rsa.ExportSubjectPublicKeyInfoPem();
        }

        public static string ExportPrivateKeyToPem(X509Certificate2 cert)
        {
            if (!cert.HasPrivateKey)
                throw new InvalidOperationException("No private key found in certificate: " + cert.Subject);

            using var tmpCert = ToExportable(cert);
            using var rsa = (AsymmetricAlgorithm)tmpCert.GetRSAPrivateKey() ?? tmpCert.GetECDsaPrivateKey();
            if (rsa is RSACng rsaCng)
            {
                using var rsaTemp = ToRsa(rsaCng);
                return rsaTemp.ExportPkcs8PrivateKeyPem();
            }

            return rsa.ExportPkcs8PrivateKeyPem();
        }

        private static X509Certificate2 ToExportable(X509Certificate2 cert)
        {
            var tmpPwd = Guid.NewGuid().ToString("N");
            var content = cert.Export(X509ContentType.Pkcs12, tmpPwd);
            return new X509Certificate2(
                content,
                tmpPwd,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        }

        private static RSA ToRsa(RSACng cng)
        {
            var parameters = cng.ExportParameters(true);
            var rsaTemp = RSA.Create();
            try
            {
                rsaTemp.ImportParameters(parameters);
            }
            catch
            {
                rsaTemp.Dispose();
                throw;
            }
        }
    }
}