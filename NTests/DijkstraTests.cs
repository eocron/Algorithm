using System.Linq;
using System.Text.RegularExpressions;
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
        public void Test(int[] pathToRome, int minLength)
        {
            var graph = ParsePathToRome(pathToRome);
            var result = DijkstraAlgorithm<int, int>.Search(0,
                x => graph.OutEdges(x).Select(y => y.Target),
                x => 0,
                (x, y) => x.Weight + 1);
            Assert.AreEqual(minLength, result.Weights[pathToRome.Length - 1]);
        }


        //private static AdjacencyGraph<int, Edge<int>> Parse(string input)
        //{
        //    var result = new AdjacencyGraph<int, Edge<int>>();
        //    foreach (var m in Regex.Matches(input, @"(?<edge>\d+\s*\-\>\s*\d+)|(?<vertex>\d+)").Cast<Match>())
        //    {
        //        if (m.Groups["vertex"].Success)
        //        {
        //            result.AddVertex(int.Parse(m.Groups["vertex"].Value));
        //        }
        //        else if (m.Groups["edge"].Success)
        //        {
        //            var split = m.Groups["edge"].Value.Split("->");
        //            result.AddEdge(new Edge<int>(int.Parse(split[0]), int.Parse(split[1])));
        //        }
        //    }

        //    return result;
        //}

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
