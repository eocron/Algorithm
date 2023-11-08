using Eocron.Serialization.Security;
using NUnit.Framework;

namespace Eocron.Serialization.Tests
{
    [TestFixture]
    public class Aes256GcmSerializationTests : SecuredSerializationTests
    {
        public override ISerializationConverter GetConverter()
        {
            return  new Aes256GcmSerializationConverter(SerializationConverter.Json, "foobar");
        }
    }
}