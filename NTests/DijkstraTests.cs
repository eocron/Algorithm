using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Eocron.Algorithms.Graphs;
using NUnit.Framework;
using QuikGraph;
using QuikGraph.Graphviz;
using QuikGraph.Graphviz.Dot;

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
        public void PathToRome(int[] cities, int expectedMinSteps)
        {
            var graph = ParsePathToRome(cities);
            var source = 0;
            var target = cities.Length - 1;
            var result = DijkstraAlgorithm<int, int>.Search(
                source,
                x => graph.OutEdges(x).Select(y => y.Target),
                x => 0,
                (x, y) => x.Weight + 1);

            var pathToRome = result.GetPath(source, target).ToList();
            Print(graph, pathToRome);
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

        private static void Print(AdjacencyGraph<int, Edge<int>> graph)
        {
            var g = new GraphvizAlgorithm<int, Edge<int>>(graph);
            var dot = g.Generate();
            var uri = "https://dreampuf.github.io/GraphvizOnline/#" + Uri.EscapeDataString(dot);
            Console.WriteLine(uri);
            Console.WriteLine(dot);
        }

        private static void Print(AdjacencyGraph<int, Edge<int>> graph, List<int> path)
        {
            var g = new GraphvizAlgorithm<int, Edge<int>>(graph);
            g.ImageType = GraphvizImageType.Svg;
            g.CommonVertexFormat.Shape = GraphvizVertexShape.Circle;
            g.CommonEdgeFormat.Direction = GraphvizEdgeDirection.Forward;

            g.FormatVertex += (_, args) =>
            {
                if (path.Contains(args.Vertex))
                {
                    args.VertexFormat.FontColor = GraphvizColor.Green;
                    args.VertexFormat.StrokeColor = GraphvizColor.Green;
                }

                if (args.Vertex == path.Last())
                {
                    args.VertexFormat.Shape = GraphvizVertexShape.DoubleCircle;
                }
            };

            g.FormatEdge += (_, args) =>
            {
                var ids = path.IndexOf(args.Edge.Source);
                var idt = path.IndexOf(args.Edge.Target);
                if (ids >=0 && idt >= 0 && ids+1 == idt)
                {
                    args.EdgeFormat.FontColor = GraphvizColor.Green;
                    args.EdgeFormat.StrokeColor = GraphvizColor.Green;
                }
            };
            var dot = g.Generate();
            var uri = "https://dreampuf.github.io/GraphvizOnline/#" + Uri.EscapeDataString(dot);
            Console.WriteLine(uri);
            Console.WriteLine(dot);
        }
    }
}
