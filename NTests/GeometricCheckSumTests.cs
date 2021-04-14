using Algorithm.FileCheckSum;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace NTests
{
    [TestFixture]
    public class GeometricCheckSumTests
    {
        [Test]
        public void ExactLengthCheckSum()
        {
            var ba = new byte[30];
            ba[0] = 1;
            ba[29] = 2;
            var ms = new MemoryStream(ba);
            var lazy = new GeometricLazyCheckSum(() => { ms.Seek(0, SeekOrigin.Begin); return ms; }, 1, 2);

            var all = lazy.ToList();

            Assert.AreEqual(5, all.Count);
            CollectionAssert.AreEqual(new[] { 528, 16337, 15699857, -661954799, -89146415 }, all);
        }

        [Test]
        public void BiggerLengthCheckSum()
        {
            var ba = new byte[1 + 2 + 4 + 8 + 5];
            ba[0] = 1;
            ba[1+2] = 2;
            ba[1+2+4+8] = 3;
            var ms = new MemoryStream(ba);
            var lazy = new GeometricLazyCheckSum(() => { ms.Seek(0, SeekOrigin.Begin); return ms; }, 1, 2);

            var all = lazy.ToList();

            Assert.AreEqual(5, all.Count);
            CollectionAssert.AreEqual(new[] { 528, 16337, 15759439, -661954799, 489466130 }, all);
        }

        [Test]
        public void ZeroLengthCheckSum()
        {
            var ba = new byte[0];
            var ms = new MemoryStream(ba);
            var lazy = new GeometricLazyCheckSum(() => { ms.Seek(0, SeekOrigin.Begin); return ms; }, 1, 2);

            var all = lazy.ToList();

            Assert.AreEqual(0, all.Count);
            CollectionAssert.AreEqual(new int[0], all);
        }

        [Test]
        public void SameLengthCheckSum()
        {
            var ba = new byte[1];
            ba[0] = 1;
            var ms = new MemoryStream(ba);
            var lazy = new GeometricLazyCheckSum(() => { ms.Seek(0, SeekOrigin.Begin); return ms; }, 1, 2);

            var all = lazy.ToList();

            Assert.AreEqual(1, all.Count);
            CollectionAssert.AreEqual(new[] { 528 }, all);
        }
    }
}
