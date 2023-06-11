using System;
using System.IO;
using System.Linq;
using Eocron.Algorithms.Randoms;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class RandomTests
    {
        [Test]
        public void InfiniteStreamCheck()
        {
            var rnd = new Random(42);

            var stream = rnd.NextStream(blockLength: 10);

            var array = new byte[234];
            var result = stream.Read(array, 0, array.Length);

            Assert.AreEqual(array.Length, result);


            stream.Seek(0, SeekOrigin.Begin);
            var tmp = new byte[1];
            for (var i = 0; i < array.Length; i++)
            {
                var readtmp = stream.Read(tmp, 0, tmp.Length);
                Assert.AreEqual(tmp.Length, readtmp);
                Assert.AreEqual(array[i], tmp[0]);
            }
        }

        [Test]
        public void BoolCheck()
        {
            var rnd = new Random(42);
            var res = new double[2];

            var count = 10000;
            for (var i = 0; i < count; i++) res[rnd.NextBool() ? 1 : 0]++;
            //almost 50/50 results, so distribution is linear.
            Assert.AreEqual(res[0] / count, res[1] / count, 0.01d);
        }

        [Test]
        [TestCase(10, new[] { '0', '1' })]
        //[TestCase(0, new[] { '0', '1' })]
        public void StringCheck(int strSize, char[] domain)
        {
            var rnd = new Random(42);
            var str = rnd.NextString(strSize, domain);
            Assert.AreEqual(strSize, str.Length);
            Assert.IsFalse(str.ToCharArray().Except(domain).Any());
        }

        [Test]
        public void StreamCheck()
        {
            var rnd = new Random(42);
            var streamSize = 123;
            var stream = rnd.NextStream(streamSize, 10);

            var array = new byte[234];
            var result = stream.Read(array, 0, array.Length);

            Assert.AreEqual(streamSize, result);


            stream.Seek(0, SeekOrigin.Begin);
            var tmp = new byte[1];
            for (var i = 0; i < streamSize; i++)
            {
                var readtmp = stream.Read(tmp, 0, tmp.Length);
                Assert.AreEqual(tmp.Length, readtmp);
                Assert.AreEqual(array[i], tmp[0]);
            }
        }
    }
}