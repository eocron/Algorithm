using NUnit.Framework;
using System.Collections.Generic;
using Algorithm.Tree;
using System.Linq;

namespace NTests
{
    [TestFixture]
    public class TreeTraversalTests
    {
        //private Dictionary<char, List<char>> Tree = new Dictionary<char, List<char>> { 
        //    { 'D', new List<char> { 'C', 'F' } },
        //    { 'C', new List<char> { 'A', 'B' } },
        //    { 'F', new List<char> { 'E', 'G' } }
        //};

        private Dictionary<char, List<char>> Tree = new Dictionary<char, List<char>> {
            { 'F', new List<char> { 'B', 'G' } },
            { 'B', new List<char> { 'A', 'D' } },
            { 'D', new List<char> { 'C', 'E' } },
                        { 'G', new List<char> { 'I' } },
                                    { 'I', new List<char> { 'H' } }
        };

        private IEnumerable<char> GetChildren(char c)
        {
            List<char> res;
            if (Tree.TryGetValue(c, out res))
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
