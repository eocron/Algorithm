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
        public void ReverseInOrder_RNL()
        {
            var root = 'F';
            var result = new string(root.TraverseReverseInOrder(GetChildren).ToArray());
            Assert.AreEqual("FGIHBDECA", result);
        }

        [Test]
        public void PreOrder_NLR()
        {
            var root = 'F';
            var result = new string(root.TraversePreOrder(GetChildren).ToArray());
            Assert.AreEqual("FBADCEGIH", result);
        }

        [Test]
        public void PostOrder_LRN()
        {
            var root = 'F';
            var result = new string(root.TraversePostOrder(GetChildren).ToArray());
            Assert.AreEqual("ACEDBHIGF", result);
        }

        [Test]
        public void LevelOrder_BFS()
        {
            var root = 'F';
            var result = new string(root.TraverseLevelOrder(GetChildren).ToArray());
            Assert.AreEqual("FBGADICEH", result);
        }
    }
}
