﻿using System;
using System.Collections.Generic;
using System.Linq;
using Eocron.Algorithms.Graphs;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using QuikGraph;
using QuikGraph.Graphviz;
using QuikGraph.Graphviz.Dot;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public sealed class DijkstraTests
    {
        [Test]
        public void Empty()
        {
            var result = new InfiniteDijkstraAlgorithm<int, int>(
                _ => null,
                _ => 0,
                (x, _) => x.Weight + 1);
            result.Search(0);

            ClassicAssert.AreEqual(0, result.GetWeight(0));
            ClassicAssert.AreEqual(0, result.GetPathFromSourceToTarget().Count());
            ClassicAssert.IsFalse(result.IsTargetFound);
        }

        [Test]
        public void Cyclic()
        {
            var graph = new AdjacencyGraph<int, Edge<int>>();
            var count = 4;
            for (var i = 0; i < count; i++) graph.AddVertex(i);
            for (var i = 0; i < count; i++)
            for (var j = 0; j < count; j++)
                graph.AddEdge(new Edge<int>(i, j));
            var source = 0;
            var target = count - 1;
            var result = new InfiniteDijkstraAlgorithm<int, int>(
                x => graph.OutEdges(x).Select(y => y.Target),
                _ => 0,
                (x, _) => x.Weight + 1);
            result.Search(source);
            var pathToRome = result.GetPath(source, target).ToList();
            ClassicAssert.AreEqual(1, result.GetWeight(target));
            ClassicAssert.AreEqual(new[] { source, target }, pathToRome);
            Print(graph, pathToRome);
        }

        [Test]
        [TestCase(new[] { 1, 1, 1, 1 }, 3)]
        [TestCase(new[] { 2, 1, 1, 1 }, 2)]
        [TestCase(new[] { 1, 2, 3, 4 }, 2)]
        [TestCase(new[] { 4, 3, 2, 1 }, 1)]
        [TestCase(new[] { 2, 2, 1, 3, 3, 2, 1 }, 3)]
        [TestCase(new[] { 2, 0, 1, 1 }, 1)]
        public void PathToNearCity(int[] cities, int expectedMinSteps)
        {
            var graph = ParsePathToRome(cities);
            var source = 0;
            var targets = cities
                .Select((x, i) => new { x, i })
                .Where(x => x.x == 0)
                .Select(x => x.i)
                .Concat(new[] { cities.Length - 1 })
                .ToList();
            var result = new InfiniteDijkstraAlgorithm<int, int>(
                x => graph.OutEdges(x).Select(y => y.Target),
                _ => 0,
                (x, _) => x.Weight + 1);
            result.Search(source);
            var target = targets.OrderBy(x => result.GetWeight(x)).First();
            var pathToRome = result.GetPath(source, target).ToList();
            Print(graph, pathToRome);
            ClassicAssert.AreEqual(expectedMinSteps, result.GetWeight(target));
        }

        [Test]
        [TestCase(new[] { 1, 1, 1, 1 }, 3)]
        [TestCase(new[] { 2, 1, 1, 1 }, 2)]
        [TestCase(new[] { 1, 2, 3, 4 }, 2)]
        [TestCase(new[] { 4, 3, 2, 1 }, 1)]
        [TestCase(new[] { 2, 2, 1, 3, 3, 2, 1 }, 3)]
        [TestCase(new[] { 2, 0, 1, 1 }, 2)]
        public void PathToRome(int[] cities, int expectedMinSteps)
        {
            var graph = ParsePathToRome(cities);
            var source = 0;
            var result = new InfiniteDijkstraAlgorithm<int, int>(
                x => graph.OutEdges(x).Select(y => y.Target),
                _ => 0,
                (x, _) => x.Weight + 1,
                buildShortestPathTree: true);
            result.Search(source);
            var target = cities.Length - 1;
            var pathToRome = result.GetPath(source, target).ToList();
            Print(graph, pathToRome);
            ClassicAssert.AreEqual(expectedMinSteps, result.GetWeight(target));
        }

        [Test]
        [TestCase("kitten", "sitting", 3)]
        [TestCase("kitten", "kitting", 2)]
        [TestCase("hello", "kelm", 3)]
        [TestCase("asetbaeaefasdfsa", "asdfaew", 12)]
        [TestCase("aaaa", "a", 3)]
        [TestCase("a", "a", 0)]
        [TestCase("a", "b", 1)]
        public void LevenstainDistance(string sourceStr, string targetStr, int expectedMinDistance)
        {
            var source = Tuple.Create(0, 0);
            var target = Tuple.Create(sourceStr.Length, targetStr.Length);
            var result = new InfiniteDijkstraAlgorithm<Tuple<int, int>, int>(
                x =>
                {
                    var list = new List<Tuple<int, int>>();
                    if (x.Item2 < targetStr.Length)
                        list.Add(Tuple.Create(x.Item1, x.Item2 + 1));
                    if (x.Item1 < sourceStr.Length)
                        list.Add(Tuple.Create(x.Item1 + 1, x.Item2));
                    if (x.Item1 < sourceStr.Length && x.Item2 < targetStr.Length)
                        list.Add(Tuple.Create(x.Item1 + 1, x.Item2 + 1));
                    return list;
                },
                _ => 0,
                (xw, y) =>
                {
                    var x = xw.Vertex;
                    if (x.Item1 < sourceStr.Length
                        && x.Item2 < targetStr.Length
                        && sourceStr[x.Item1] == targetStr[x.Item2]
                        && y.Item1 - 1 == x.Item1
                        && y.Item2 - 1 == x.Item2)
                        return xw.Weight;

                    return xw.Weight + 1;
                },
                buildShortestPathTree: true);
            result.Search(source);

            var minDistance = result.GetWeight(target);
            ClassicAssert.AreEqual(expectedMinDistance, minDistance);
        }

        [Test]
        [Explicit]
        public void Debug()
        {
            var rnd = new Random(42);
            var cities = Enumerable.Range(0, 30).Select(_ => rnd.Next(0, 10)).ToList();
            var graph = ParsePathToRome(cities);
            var source = 0;
            var targets = cities
                .Select((x, i) => new { x, i })
                .Where(x => x.x == 0)
                .Select(x => x.i)
                .Concat(new[] { cities.Count - 1 })
                .ToList();
            var result = new InfiniteDijkstraAlgorithm<int, int>(
                x => graph.OutEdges(x).Select(y => y.Target),
                _ => 0,
                (x, _) => x.Weight + 1,
                buildShortestPathTree: true);
            result.Search(source);


            //Print(graph, null);

            var target = graph.VertexCount - 1; //targets.OrderBy(x => result.GetWeight(x)).First();
            var pathToRome = result.GetPath(source, target).ToList();
            Print(graph, pathToRome);
        }

        /// <summary>
        ///     Path to rome is a game. Each index represents city, each value represents range of adjacent cities (i.e from i+1 to
        ///     i+array[i]).
        ///     Last index is Rome. You need to travel from first index to last in least amount of steps.
        ///     Example:
        ///     1  2  1  3  1  1  1
        ///     \/
        ///     1  2  1  3  1  1  1
        ///     \_/__/
        ///     1  2  1  3  1  1  1
        ///     \_/__/__/
        ///     Which is 3 steps.
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static AdjacencyGraph<int, Edge<int>> ParsePathToRome(IList<int> paths)
        {
            var result = new AdjacencyGraph<int, Edge<int>>();
            for (var i = 0; i < paths.Count; i++)
            {
                result.AddVertex(i);
                for (var j = i + 1; j < paths.Count && j <= i + paths[i]; j++)
                {
                    result.AddVertex(j);
                    result.AddEdge(new Edge<int>(i, j));
                }
            }

            return result;
        }


        private static void Print(AdjacencyGraph<int, Edge<int>> graph, List<int> path)
        {
            var g = new GraphvizAlgorithm<int, Edge<int>>(graph);
            g.ImageType = GraphvizImageType.Svg;
            g.CommonVertexFormat.Shape = GraphvizVertexShape.Circle;
            g.CommonEdgeFormat.Direction = GraphvizEdgeDirection.Forward;

            if (path != null && path.Any())
            {
                g.FormatVertex += (_, args) =>
                {
                    if (path.Contains(args.Vertex))
                    {
                        args.VertexFormat.FontColor = GraphvizColor.Green;
                        args.VertexFormat.StrokeColor = GraphvizColor.Green;
                    }

                    if (args.Vertex == path.Last()) args.VertexFormat.Shape = GraphvizVertexShape.DoubleCircle;
                };

                g.FormatEdge += (_, args) =>
                {
                    var ids = path.IndexOf(args.Edge.Source);
                    var idt = path.IndexOf(args.Edge.Target);
                    if (ids >= 0 && idt >= 0 && ids + 1 == idt)
                    {
                        args.EdgeFormat.FontColor = GraphvizColor.Green;
                        args.EdgeFormat.StrokeColor = GraphvizColor.Green;
                    }
                };
            }

            var dot = g.Generate();
            var uri = "https://dreampuf.github.io/GraphvizOnline/#" + Uri.EscapeDataString(dot);
            Console.WriteLine(uri);
        }
    }
}