using System.Linq;
using Eocron.Algorithms.Graphs;
using NUnit.Framework;
using QuikGraph;

namespace NTests
{
    [TestFixture]
    public sealed class DijkstraTests
    {
        [Test]
        [TestCase(new[] {1, 1, 1, 1}, 3)]
        [TestCase(new[] {2, 1, 1, 1}, 2)]
        [TestCase(new[] {1, 2, 3, 4}, 2)]
        [TestCase(new[] {4, 3, 2, 1}, 1)]
        public void PathToRome(int[] pathToRome, int expectedMinSteps)
        {
            var graph = ParsePathToRome(pathToRome);
            var source = 0;
            var target = pathToRome.Length - 1;
            var result = DijkstraAlgorithm<int, int>.Search(
                source,
                x => graph.OutEdges(x).Select(y => y.Target),
                x => 0,
                (x, y) => x.Weight + 1);
            Assert.AreEqual(expectedMinSteps, result.Weights[target]);
        }

        /// <summary>
        /// Path to rome is a game. Each index represents city, each value represents range of adjacent cities (i.e from i+1 to i+array[i]).
        /// Last index is Rome. You need to travel from first index to last in least amount of steps.
        /// Example:
        /// 1  2  1  3  1  1  1
        ///  \/
        /// 
        /// 1  2  1  3  1  1  1
        ///    \_/__/
        /// 
        /// 1  2  1  3  1  1  1
        ///          \_/__/__/
        ///
        /// Which is 3 steps.
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        private static AdjacencyGraph<int, Edge<int>> ParsePathToRome(int[] paths)
        {
            var result = new AdjacencyGraph<int, Edge<int>>();
            for (int i = 0; i < paths.Length; i++)
            {
                result.AddVertex(i);
                for (int j = i+1; j < paths.Length && j <= i+paths[i]; j++)
                {
                    result.AddVertex(j);
                    result.AddEdge(new Edge<int>(i,j));
                }
            }

            return result;
        }
    }
}
