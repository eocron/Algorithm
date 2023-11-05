using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Eocron.Serialization.Security
{
    public static class PasswordDerivationHelper
    {
        private static readonly SecureRandom Random = new SecureRandom();
    
        public static PasswordDerivative GenerateFrom(string password, int saltByteSize, int keyByteSize, int iterations = 10001)
        {
            var salt = new byte[saltByteSize];
            Random.NextBytes(salt);
            var generator = new Pkcs5S2ParametersGenerator();
            generator.Init(
                PbeParametersGenerator.Pkcs5PasswordToBytes(password.ToCharArray()),
                salt,
                iterations);
            var hash = ((KeyParameter)generator.GenerateDerivedMacParameters(keyByteSize * 8)).GetKey();
            return new PasswordDerivative()
            {
                Hash = hash,
                Salt = salt
            };
        }
    }
}