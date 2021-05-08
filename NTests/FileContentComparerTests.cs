using Eocron.Algorithms;
using NUnit.Framework;
using System;
using System.IO;

namespace NTests
{
    [TestFixture]
    public class FileContentComparerTests
    {
        private string _smallFile;
        private string _bigFile;
        [OneTimeSetUp]
        public void SetUp()
        {
            var rnd = new Random(42);
            _smallFile = Path.GetTempFileName();
            _bigFile = Path.GetTempFileName();

            rnd.NextFile(_smallFile, 100);
            rnd.NextFile(_bigFile, 10_000_000);
        }
        [OneTimeTearDown]
        public void TearDown()
        {
            if (_smallFile != null)
                File.Delete(_smallFile);

            if (_bigFile != null)
                File.Delete(_bigFile);
        }

        [Test]
        public void Equal()
        {
            var cmp = FileContentComparer.Default;

            Assert.IsTrue(cmp.Equals(_smallFile, _smallFile));
            Assert.IsTrue(cmp.Equals(_bigFile, _bigFile));
            Assert.AreEqual(cmp.GetHashCode(_smallFile), cmp.GetHashCode(_smallFile));
            Assert.AreEqual(cmp.GetHashCode(_bigFile), cmp.GetHashCode(_bigFile));
        }

        [Test]
        public void NotEqual()
        {
            var cmp = FileContentComparer.Default;

            Assert.IsFalse(cmp.Equals(_smallFile, _bigFile));
            Assert.AreNotEqual(cmp.GetHashCode(_smallFile), cmp.GetHashCode(_bigFile));
        }
    }
}
