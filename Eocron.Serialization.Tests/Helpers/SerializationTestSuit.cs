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
            var expected = CreateTestModel(path);
            var converter = GetConverter();
            var serialized = converter.SerializeToString(expected);
            var actual = converter.Deserialize<T>(serialized);

            try
            {
                actual.Should().BeEquivalentTo(expected);
            }
            catch
            {
                TryPrint(converter, "EXPECTED:", expected);
                TryPrint(converter, "ACTUAL:", actual);
                throw;
            }
        }

        public void AssertSerializeAndDeserializeByBytes(string path)
        {
            var model = CreateTestModel(path);
            var converter = GetConverter();
            var serialized = converter.SerializeToBytes(model);
            var deserialized = converter.Deserialize<T>(serialized);
            deserialized.Should().BeEquivalentTo(model);
        }

        public void AssertDeserializedFromTextModelEqualTo(string path)
        {
            var expectedText = TestDataHelper.ReadAllText(path);
            var expected = CreateTestModel(path);
            var converter = GetConverter();
            var actual = converter.Deserialize<T>(expectedText);
            try
            {
                actual.Should().BeEquivalentTo(expected);
            }
            catch
            {
                Console.WriteLine("EXPECTED:");
                Console.WriteLine(expectedText);

                TryPrint(converter, "ACTUAL:", actual);
                throw;
            }
        }

        private void TryPrint(ISerializationConverter converter, string header, T obj)
        {
            try
            {
                var text = converter.SerializeToString(obj);
                Console.WriteLine(header);
                Console.WriteLine(text);
            }
            catch(Exception ex)
            {
                Console.WriteLine(header);
                Console.WriteLine(ex.Message);
            }
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

        public void AssertSerializedBytesEqualTo(string path, bool base64Print)
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
                if (!base64Print)
                {
                    Console.WriteLine("EXPECTED:");
                    Console.WriteLine(Encoding.UTF8.GetString(expectedBytes));
                    Console.WriteLine("ACTUAL:");
                    Console.WriteLine(Encoding.UTF8.GetString(actualBytes));
                }
                else
                {
                    Console.WriteLine("EXPECTED:");
                    Console.WriteLine(Convert.ToBase64String(expectedBytes));
                    Console.WriteLine("ACTUAL:");
                    Console.WriteLine(Convert.ToBase64String(actualBytes));
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