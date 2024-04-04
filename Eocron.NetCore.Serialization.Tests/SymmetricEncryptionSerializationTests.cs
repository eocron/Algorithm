using Eocron.NetCore.Serialization.Security;
using Eocron.Serialization;
using Eocron.Serialization.Json;
using NUnit.Framework;

namespace Eocron.NetCore.Serialization.Tests
{
    [TestFixture]
    public class SymmetricEncryptionSerializationTests : EncryptionSerializationTestsBase
    {
        public override ISerializationConverter GetConverter()
        {
            return  new SymmetricEncryptionSerializationConverter(SerializationConverterJson.Json, "foobar");
        }
    }
}