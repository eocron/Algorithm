using System.Collections.Generic;
using System.Linq;
using Eocron.Algorithms.Tree;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class TreeTraversalTests
    {
        /// <summary>
        ///     F
        ///     / | \
        ///     B  J  G
        ///     / \     \
        ///     A   D     I
        ///     / \     \
        ///     C   E     H
        /// </summary>
        /// <returns></returns>
        private Dictionary<char, char[]> GetTestTree()
        {
            return new Dictionary<char, char[]>
            {
                { 'F', new[] { 'B', 'J', 'G' } },
                { 'B', new[] { 'A', 'D' } },
                { 'D', new[] { 'C', 'E' } },
                { 'G', new[] { 'I' } },
                { 'I', new[] { 'H' } }
            };
        }

        private IEnumerable<char> GetChildren(char c)
        {
            char[] res;
            if (GetTestTree().TryGetValue(c, out res))
                return res;
            return null;
        }

        [Test]
        [TestCase('F', TraversalKind.ReverseInOrder, "FGIHJBDECA")]
        [TestCase('F', TraversalKind.PreOrder, "FBADCEJGIH")]
        [TestCase('F', TraversalKind.PostOrder, "ACEDBJHIGF")]
        [TestCase('F', TraversalKind.LevelOrder, "FBJGADICEH")]
        public void Traversal(char root, TraversalKind kind, string expected)
        {
            var actual = new string(root.Traverse(GetChildren, kind).ToArray());
            ClassicAssert.AreEqual(expected, actual);
        }
    }
}