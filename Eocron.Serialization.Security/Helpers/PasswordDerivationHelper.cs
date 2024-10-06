using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Eocron.Serialization.Security.Helpers
{
    public static class PasswordDerivationHelper
    {
        private static readonly SecureRandom Random = new();

        private static readonly byte[] DefaultKeySalt =
        {
            104, 95,  254, 255,
            128, 42,  64,  55,
            214, 255, 154, 36,
            133, 80,  11,  172,
            19,  35,  166, 12,
            10,  96,  60,  59,
            225, 146, 151, 67,
            85,  144, 122, 19
        };

        private const int DefaultIterationCount = 10001;

        public static IRentedArray<byte> CreateRandomBytes(IRentedArrayPool<byte> pool, int size)
        {
            var result = pool.RentExact(size);
            Random.NextBytes(result.Data);
            return result;
        }
        
        public static byte[] GenerateKeyFrom(string password, int keyByteSize)
        {
            return GenerateFrom(password, DefaultKeySalt, keyByteSize);
        }
        
        public static byte[] GenerateFrom(string password, byte[] salt, int keyByteSize, int iterations = DefaultIterationCount)
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