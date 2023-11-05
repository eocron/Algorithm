using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Eocron.Serialization.Security
{
    public static class PasswordDerivationHelper
    {
        public static byte[] GenerateFrom(string password, byte[] salt, int keyByteSize, int iterations = 10001)
        {
            var generator = new Pkcs5S2ParametersGenerator();
            generator.Init(
                PbeParametersGenerator.Pkcs5PasswordToBytes(password.ToCharArray()),
                salt,
                iterations);
            var hash = ((KeyParameter)generator.GenerateDerivedMacParameters(keyByteSize * 8)).GetKey();
            return hash;
        }
    }
}