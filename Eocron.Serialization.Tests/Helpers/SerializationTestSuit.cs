using System.Text;
using FluentAssertions;

namespace Eocron.Serialization.Tests.Helpers
{
    public abstract class SerializationTestSuit<T>
    {
        public abstract ISerializationConverter GetConverter();
        public abstract T CreateTestModel(string path);
        
        public void AssertSerializeAndDeserializeByText(string path)
        {
            var model = CreateTestModel(path);
            var converter = GetConverter();
            var serialized = converter.SerializeToString(model);
            var deserialized = converter.Deserialize<T>(serialized);

            try
            {
                model.Should().BeEquivalentTo(deserialized);
            }
            catch
            {
                Console.WriteLine("ACTUAL:");
                Console.WriteLine(serialized);
                throw;
            }
        }

        public void AssertDeserializedModelEqualTo(string path)
        {
            var expectedXml = TestDataHelper.ReadAllText(path);
            var expected = CreateTestModel(path);

            var actual = GetConverter().Deserialize<T>(expectedXml);
            actual.Should().BeEquivalentTo(expected);
        }

        public void AssertSerializedTextEqualTo(string path)
        {
            var expectedXml = TestDataHelper.ReadAllText(path);
            var expected = CreateTestModel(path);

            var actualXml = GetConverter().SerializeToString(expected);
            AssertEqualSerializedText(expectedXml, actualXml);
        }

        public void AssertSerializedBytesEqualTo(string path, bool printAsBase64)
        {
            var expectedBytes = TestDataHelper.ReadAllBytes(path);
            var expected = CreateTestModel(path);


            var actualBytes = GetConverter().SerializeToBytes(expected);
            try
            {
                actualBytes.Should().BeEquivalentTo(expectedBytes);
            }
            catch
            {
                if (printAsBase64)
                {
                    Console.WriteLine("EXPECTED:");
                    Console.WriteLine(Convert.ToBase64String(expectedBytes));
                    Console.WriteLine("ACTUAL:");
                    Console.WriteLine(Convert.ToBase64String(actualBytes));
                }
                else
                {
                    Console.WriteLine("EXPECTED:");
                    Console.WriteLine(Encoding.UTF8.GetString(expectedBytes));
                    Console.WriteLine("ACTUAL:");
                    Console.WriteLine(Encoding.UTF8.GetString(actualBytes));
                }
                throw;
            }
        }

        private static void AssertEqualSerializedText(string expected, string actual)
        {
            try
            {
                expected.Should().BeEquivalentTo(actual);
            }
            catch
            {
                Console.WriteLine("EXPECTED:");
                Console.WriteLine(expected);
                Console.WriteLine("ACTUAL:");
                Console.WriteLine(actual);
                throw;
            }
        }
    }
}