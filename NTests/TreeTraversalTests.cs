using NUnit.Framework;
using System.Collections.Generic;
using Algorithm.Tree;
using System.Linq;

namespace NTests
{
    [TestFixture]
    public class TreeTraversalTests
    {
        /// <summary>
        ///      F
        ///     / \
        ///    B   G
        ///   / \   \
        ///  A   D   I
        ///     / \   \
        ///    C   E   H
        /// </summary>
        /// <returns></returns>
        private Dictionary<char, char[]> GetTestTree()
        {
            return new Dictionary<char, char[]>()
            {
                {'F', new[]{'B','G'}},
                {'B', new[]{'A','D'}},
                {'D', new[]{'C','E'}},
                {'G', new[]{'I'}},
                {'I', new[]{'H'}},
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
        [TestCase('F', TraversalKind.ReverseInOrder, "FGIHBDECA")]
        [TestCase('F', TraversalKind.PreOrder, "FBADCEGIH")]
        [TestCase('F', TraversalKind.PostOrder, "ACEDBHIGF")]
        [TestCase('F', TraversalKind.LevelOrder, "FBGADICEH")]
        public void Traversal(char root, TraversalKind kind, string expected)
        {
            var actual = new string(root.Traverse(GetChildren, kind).ToArray());
            Assert.AreEqual(expected, actual);
        }
    }
}
