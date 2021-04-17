using Eocron.Algorithms.ByteArray;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NTests
{
    [TestFixture]
    public class ByteArrayTests
    {
        private static Random _rnd = new Random(42);

        private static IEnumerable<TestCaseData> GetAreEqualTests()
        {
            yield return Eq(1).SetName("+1b");
            yield return Eq(20000).SetName("+20000b");
            yield return Eq(20 * 1024 * 1024).SetName("+20mb");
            yield return new TestCaseData(Array.Empty<byte>(), Array.Empty<byte>()).SetName("+empty");
            yield return new TestCaseData(null, null).SetName("+null");
        }

        private static TestCaseData Eq(int size)
        {
            var a = GetBytes(size);
            var b = new byte[size];
            Array.Copy(a, b, size);
            return new TestCaseData(a, b);
        }

        private static IEnumerable<TestCaseData> GetAreNotEqualTests()
        {
            yield return new TestCaseData(new byte[] { 1}, new byte[] { 2}).SetName("-1b");
            yield return new TestCaseData(GetBytes(20000), GetBytes(20000)).SetName("-20000b");
            yield return new TestCaseData(GetBytes(20 * 1024 * 1024), GetBytes(20 * 1024 * 1024)).SetName("-20mb");
            yield return new TestCaseData(Array.Empty<byte>(), null).SetName("-rnull");
            yield return new TestCaseData(null, Array.Empty<byte>()).SetName("-lnull");
        }

        private static byte[] GetBytes(int size)
        {
            var res = new byte[size];
            _rnd.NextBytes(res);
            return res;
        }
       
        [Test]
        [TestCaseSource(nameof(GetAreEqualTests))]
        public void AreEqual(byte[] a, byte[] b)
        {
            var cmp = new ByteArrayEqualityComparer();
            Assert.IsTrue(cmp.Equals(a, b));
            Assert.AreEqual(cmp.GetHashCode(a), cmp.GetHashCode(b));
        }

        [Test]
        [TestCaseSource(nameof(GetAreNotEqualTests))]
        public void AreNotEqual(byte[] a, byte[] b)
        {
            var cmp = new ByteArrayEqualityComparer();
            Assert.IsFalse(cmp.Equals(a, b));
            Assert.AreNotEqual(cmp.GetHashCode(a), cmp.GetHashCode(b));
        }
    }
}
