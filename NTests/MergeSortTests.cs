using System.Linq;
using Eocron.Algorithms.Sorted;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class MergeSortTests
    {
        [TestCase(new int[0])]
        [TestCase(new[] { 1 })]
        [TestCase(new[] { 1, 2, 3, 4 })]
        [TestCase(new[] { 4, 3, 2, 1 })]
        [TestCase(new[] { 1, 2, 3, 4, 5 })]
        [TestCase(new[] { 5, 4, 3, 2, 1 })]
        [TestCase(new[] { 1, 1, 1 })]
        public void CheckInMemory(int[] data)
        {
            var expected = data.OrderBy(x => x).ToList();
            var actual = data.MergeOrderBy(x => x, new InMemoryEnumerableStorage<int>(), minimalChunkSize: 2);
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestCase(new int[0])]
        [TestCase(new[] { 1 })]
        [TestCase(new[] { 1, 2, 3, 4 })]
        [TestCase(new[] { 4, 3, 2, 1 })]
        [TestCase(new[] { 1, 2, 3, 4, 5 })]
        [TestCase(new[] { 5, 4, 3, 2, 1 })]
        [TestCase(new[] { 1, 1, 1 })]
        public void CheckPersistent(int[] data)
        {
            using var storage = new JsonEnumerableStorage<int>();
            var expected = data.OrderBy(x => x).ToList();
            var actual = data.MergeOrderBy(x => x, storage, minimalChunkSize: 2).ToList();
            CollectionAssert.AreEqual(expected, actual, storage.TempFolder);
        }
    }
}
