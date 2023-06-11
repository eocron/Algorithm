using Eocron.Algorithms.ReadableBytes;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class ReadableFileSizeTests
    {
        [Test]
        [TestCase(-100L, "-100 B")]
        [TestCase(0L, "0 B")]
        [TestCase(1L, "1 B")]
        [TestCase(1234L, "1.205 KB")]
        [TestCase(1234567L, "1.177 MB")]
        [TestCase(1234567890L, "1.149 GB")]
        [TestCase(1234567890123L, "1.122 TB")]
        [TestCase(1234567890123456L, "1.096 PB")]
        [TestCase(1234567890123456789L, "1.070 EB")]
        public void Check(long size, string expected)
        {
            var actual = size.ToReadableSizeString();
            Assert.AreEqual(expected, actual);
        }
    }
}
