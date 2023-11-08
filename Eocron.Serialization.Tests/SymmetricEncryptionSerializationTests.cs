using Eocron.Serialization.Security;
using NUnit.Framework;

namespace Eocron.Serialization.Tests
{
    [TestFixture]
    public class SymmetricEncryptionSerializationTests : EncryptionSerializationTestsBase
    {
        public override ISerializationConverter GetConverter()
        {
            return  new SymmetricEncryptionSerializationConverter(SerializationConverter.Json, "foobar");
        }
    }
}