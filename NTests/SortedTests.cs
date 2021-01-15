using NUnit.Framework;
using Algorithm.Sorted;
namespace NTests
{
    [TestFixture]
    public class SortedTests
    {
        [Test]
        [TestCase("bbbbccccdddd", 'a', -1)]
        [TestCase("bbbbccccdddd", 'b', 2)]
        [TestCase("bbbbccccdddd", 'c', 5)]
        [TestCase("bbbbccccdddd", 'd', 8)]
        [TestCase("bbbbccccdddd", 'e', -1)]
        [TestCase("bcd", 'b', 0)]
        [TestCase("bcd", 'd', 2)]
        [TestCase("aa", 'a', 0)]
        [TestCase("a", 'a', 0)]
        [TestCase("", 'a', -1)]
        public void BinarySearch(string list, char value, int expected_index)
        {
            var actual = list.ToCharArray().BinarySearchIndexOf(value);
            Assert.AreEqual(expected_index, actual);
        }

        [Test]
        [TestCase("bbbbccccdddd", 'a', 0)]
        [TestCase("bbbbccccdddd", 'b', 4)]
        [TestCase("bbbbccccdddd", 'c', 8)]
        [TestCase("bbbbccccdddd", 'd', 12)]
        [TestCase("bbbbccccdddd", 'e', 12)]
        [TestCase("bcd", 'b', 1)]
        [TestCase("bcd", 'd', 3)]
        [TestCase("aa", 'a', 2)]
        [TestCase("a", 'a', 1)]
        [TestCase("", 'a', 0)]
        public void UpperBound(string list, char value, int expected_index)
        {
            var actual = list.ToCharArray().UpperBoundIndexOf(value);
            Assert.AreEqual(expected_index, actual);
        }

        [Test]
        [TestCase("bbbbccccdddd", 'a', -1)]
        [TestCase("bbbbccccdddd", 'b', -1)]
        [TestCase("bbbbccccdddd", 'c', 3)]
        [TestCase("bbbbccccdddd", 'd', 7)]
        [TestCase("bbbbccccdddd", 'e', 11)]
        [TestCase("bcd", 'b', -1)]
        [TestCase("bcd", 'd', 1)]
        [TestCase("aa", 'a', -1)]
        [TestCase("a", 'a', -1)]
        [TestCase("", 'a', -1)]
        public void LowerBound(string list, char value, int expected_index)
        {
            var actual = list.ToCharArray().LowerBoundIndexOf(value);
            Assert.AreEqual(expected_index, actual);
        }
    }
}
