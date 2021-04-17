using Eocron.Algorithms.FileCheckSum;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace NTests
{
    [TestFixture]
    public class PartitionedLazyCheckSumTests
    {
        [Test]
        public void ExactLengthCheckSum()
        {
            var ba = new byte[30];
            ba[0] = 1;
            ba[29] = 2;
            var ms = new MemoryStream(ba);
            var lazy = new LazyCheckSum<int>(()=> { ms.Seek(0, SeekOrigin.Begin); return ms; }, new PartitionedCheckSum(10));

            var all = lazy.ToList();

            Assert.AreEqual(3, all.Count);
            CollectionAssert.AreEqual(new [] { -679915536, -483402031, -483402029 }, all);
        }

        [Test]
        public void BiggerLengthCheckSum()
        {
            var ba = new byte[33];
            ba[0] = 1;
            ba[29] = 2;
            ba[32] = 3;
            var ms = new MemoryStream(ba);
            var lazy = new LazyCheckSum<int>(() => { ms.Seek(0, SeekOrigin.Begin); return ms; }, new PartitionedCheckSum(10));

            var all = lazy.ToList();

            Assert.AreEqual(4, all.Count);
            CollectionAssert.AreEqual(new[] { -679915536, -483402031, -483402029, 506450 }, all);
        }

        [Test]
        public void ZeroLengthCheckSum()
        {
            var ba = new byte[0];
            var ms = new MemoryStream(ba);
            var lazy = new LazyCheckSum<int>(() => { ms.Seek(0, SeekOrigin.Begin); return ms; }, new PartitionedCheckSum(10));

            var all = lazy.ToList();

            Assert.AreEqual(0, all.Count);
            CollectionAssert.AreEqual(new int[0], all);
        }

        [Test]
        public void SameLengthCheckSum()
        {
            var ba = new byte[10];
            ba[0] = 1;
            var ms = new MemoryStream(ba);
            var lazy = new LazyCheckSum<int>(() => { ms.Seek(0, SeekOrigin.Begin); return ms; }, new PartitionedCheckSum(10));

            var all = lazy.ToList();

            Assert.AreEqual(1, all.Count);
            CollectionAssert.AreEqual(new [] { -679915536 }, all);
        }
    }
}
