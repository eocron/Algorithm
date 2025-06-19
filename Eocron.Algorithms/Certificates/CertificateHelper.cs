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

        public static string ExportPrivateKeyToPem(X509Certificate2 cert)
        {
            if (!cert.HasPrivateKey)
                throw new InvalidOperationException("No private key found in certificate: " + cert.Subject);

            var rsa = cert.GetRSAPrivateKey();
            if (rsa is RSACng rsaCng)
            {
                try
                {
                    var parameters = rsaCng.ExportParameters(true);
                    using var rsaTemp = RSA.Create();
                    rsaTemp.ImportParameters(parameters);
                    return rsaTemp.ExportPkcs8PrivateKeyPem();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Private key can't be exported: " + cert.Subject, ex);
                }
            }

            return rsa.ExportPkcs8PrivateKeyPem();

        }
    }
}