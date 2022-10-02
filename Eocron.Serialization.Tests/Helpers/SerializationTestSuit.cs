using System;
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

        public void AssertSerializeAndDeserializeByBytes(string path)
        {
            var model = CreateTestModel(path);
            var converter = GetConverter();
            var serialized = converter.SerializeToBytes(model);
            var deserialized = converter.Deserialize<T>(serialized);
            model.Should().BeEquivalentTo(deserialized);
        }

        public void AssertDeserializedFromTextModelEqualTo(string path)
        {
            var expectedText = TestDataHelper.ReadAllText(path);
            var expected = CreateTestModel(path);

            var actual = GetConverter().Deserialize<T>(expectedText);
            actual.Should().BeEquivalentTo(expected);
        }

        public void AssertDeserializedFromBytesModelEqualTo(string path)
        {
            var expectedBytes = TestDataHelper.ReadAllBytes(path);
            var expected = CreateTestModel(path);

            var actual = GetConverter().Deserialize<T>(expectedBytes);
            actual.Should().BeEquivalentTo(expected);
        }

        public void AssertSerializedTextEqualTo(string path)
        {
            var expectedText = TestDataHelper.ReadAllText(path);
            var expected = CreateTestModel(path);

            var actualXml = GetConverter().SerializeToString(expected);
            AssertEqualSerializedText(expectedText, actualXml);
        }

        public void AssertSerializedBytesEqualTo(string path, bool hidePrint)
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
                if (!hidePrint)
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