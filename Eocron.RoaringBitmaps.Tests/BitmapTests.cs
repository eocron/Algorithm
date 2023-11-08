using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Eocron.RoaringBitmaps.Tests
{
    [TestFixture]
    public class BitmapTests
    {
        [Test]
        public void Serialization()
        {
            var bm = new Bitmap();
            bm.AddMany(1, 2, 3, 4);
            bm.AddMany(1000000, 1000001);
            var bytes  = bm.ToByteArray();
            bytes.Should().NotBeEmpty();
            
            var bm2 = new Bitmap(bytes);
            bm2.Should().Equal(bm);
            bm2.Should().BeEquivalentTo(bm);
        }

        [Test]
        public void SerializationSame()
        {
            var indexes = GetIndexes().ToList();
            var bm = new Bitmap();
            foreach (var i in indexes)
            {
                bm.Add(i);
            }

            indexes = indexes.Concat(indexes).ToList();
            indexes.Sort((x, y) => TestContext.CurrentContext.Random.NextDouble() > 0.5 ? 0 : 1);
            var bm2 = new Bitmap();
            foreach (var i in indexes)
            {
                bm2.Add(i);
            }
            bm.ToByteArray().Should().BeEquivalentTo(bm2.ToByteArray());
        }


        private static IEnumerable<uint> GetIndexes(int count = 10)
        {
            return Enumerable.Range(0, 10000).Select(x => (uint)TestContext.CurrentContext.Random.Next(0, 100000000)).ToList();
        }
    }
}