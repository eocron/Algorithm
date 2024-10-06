using Eocron.Algorithms.Sorted;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class ExtendedSortedTests
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
        [CancelAfter(1000)]
        public void BinarySearch(string list, char value, int expectedIndex)
        {
            var actual = list.ToCharArray().ExtendedBinarySearchIndexOf(value);
            ClassicAssert.AreEqual(expectedIndex, actual);
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
        [CancelAfter(1000)]
        public void UpperBound(string list, char value, int expectedIndex)
        {
            var actual = list.ToCharArray().ExtendedUpperBoundIndexOf(value);
            ClassicAssert.AreEqual(expectedIndex, actual);
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
        [CancelAfter(1000)]
        public void LowerBound(string list, char value, int expectedIndex)
        {
            var actual = list.ToCharArray().ExtendedLowerBoundIndexOf(value);
            ClassicAssert.AreEqual(expectedIndex, actual);
        }

        [Test]
        [TestCase("ddddccccbbbb", 'a', -1)]
        [TestCase("ddddccccbbbb", 'b', 8)]
        [TestCase("ddddccccbbbb", 'c', 5)]
        [TestCase("ddddccccbbbb", 'd', 2)]
        [TestCase("ddddccccbbbb", 'e', -1)]
        [TestCase("dcb", 'b', 2)]
        [TestCase("dcb", 'd', 0)]
        [TestCase("aa", 'a', 0)]
        [TestCase("a", 'a', 0)]
        [TestCase("", 'a', -1)]
        [CancelAfter(1000)]
        public void BinarySearchDescending(string list, char value, int expectedIndex)
        {
            var actual = list.ToCharArray().ExtendedBinarySearchIndexOf(value, descendingOrder: true);
            ClassicAssert.AreEqual(expectedIndex, actual);
        }

        [Test]
        [TestCase("ddddccccbbbb", 'a', 12)]
        [TestCase("ddddccccbbbb", 'b', 12)]
        [TestCase("ddddccccbbbb", 'c', 8)]
        [TestCase("ddddccccbbbb", 'd', 4)]
        [TestCase("ddddccccbbbb", 'e', 0)]
        [TestCase("dcb", 'b', 3)]
        [TestCase("dcb", 'd', 1)]
        [TestCase("aa", 'a', 2)]
        [TestCase("a", 'a', 1)]
        [TestCase("", 'a', 0)]
        [CancelAfter(1000)]
        public void UpperBoundDescending(string list, char value, int expectedIndex)
        {
            var actual = list.ToCharArray().ExtendedUpperBoundIndexOf(value, descendingOrder: true);
            ClassicAssert.AreEqual(expectedIndex, actual);
        }

        [Test]
        [TestCase("ddddccccbbbb", 'a', 11)]
        [TestCase("ddddccccbbbb", 'b', 7)]
        [TestCase("ddddccccbbbb", 'c', 3)]
        [TestCase("ddddccccbbbb", 'd', -1)]
        [TestCase("ddddccccbbbb", 'e', -1)]
        [TestCase("dcb", 'b', 1)]
        [TestCase("dcb", 'd', -1)]
        [TestCase("aa", 'a', -1)]
        [TestCase("a", 'a', -1)]
        [TestCase("", 'a', -1)]
        [CancelAfter(1000)]
        public void LowerBoundDescending(string list, char value, int expectedIndex)
        {
            var actual = list.ToCharArray().ExtendedLowerBoundIndexOf(value, descendingOrder: true);
            ClassicAssert.AreEqual(expectedIndex, actual);
        }
    }
}