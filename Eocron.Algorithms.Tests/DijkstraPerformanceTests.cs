using System;
using System.Linq;
using System.Threading;
using Eocron.Algorithms.Graphs;
using Eocron.Algorithms.Tests.Core;
using NUnit.Framework;
using QuikGraph;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    [Category("Performance")]
    [Explicit]
    public sealed class DijkstraPerformanceTests
    {
        [SetUp]
        public void SetUp()
        {
            var rnd = new Random(42);
            _graph = DijkstraTests.ParsePathToRome(Enumerable.Range(0, 100).Select(_ => rnd.Next(0, 10)).ToList());
        }

        private AdjacencyGraph<int, Edge<int>> _graph;

        [Test]
        public void Infinite()
        {
            var source = 0;
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            Benchmark.InfiniteMeasure(ctx =>
            {
                var result = new InfiniteDijkstraAlgorithm<int, int>(
                    x => _graph.OutEdges(x).Select(y => y.Target),
                    _ => 0,
                    (x, _) => x.Weight + 1,
                    count: _graph.VertexCount);
                result.Search(source);
                ctx.Increment();
            }, cts.Token);
        }
    }
}