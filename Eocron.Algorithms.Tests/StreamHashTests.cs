using System;
using System.IO;
using System.Threading.Tasks;
using Eocron.Algorithms.HashCode;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class StreamHashTests
    {
        [Test]
        public async Task AllTests()
        {
            var bytes = new byte[1024 * 1024];
            var rnd = new Random(42);
            rnd.NextBytes(bytes);
            var prev = (long?)null;
            for (var i = 0; i < bytes.Length; i += 149)
            {
                var ms = new MemoryStream(bytes, 0, i, false, false);

                var next = await ms.GetHashCodeAsync();
                if (prev != null)
                    Assert.AreNotEqual(prev.Value, next);

                prev = next;
            }
        }
    }
}