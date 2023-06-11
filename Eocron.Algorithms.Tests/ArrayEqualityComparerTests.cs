using System.Collections;
using System.Linq;
using Eocron.Algorithms.EqualityComparers;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class ArrayEqualityComparerTests
    {
        [Test]
        [TestCase("","", true)]
        [TestCase(null, null, true)]
        [TestCase("abc", "abc", true)]
        [TestCase("abc", "bcd", false)]
        [TestCase("abc", "abcd", false)]
        [TestCase(null, "abc", false)]
        [TestCase("abc", null, false)]
        [TestCase("", "abc", false)]
        [TestCase("abc", "", false)]
        [TestCase(new object[] { null, null}, new object[]{ null, null }, true)]
        public void Check(IEnumerable a, IEnumerable b, bool expected)
        {
            var cmp = ArrayEqualityComparer<object>.Default;
            var actual = cmp.Equals(a?.Cast<object>(), b?.Cast<object>());
            Assert.AreEqual(expected, actual);
        }
    }
}
