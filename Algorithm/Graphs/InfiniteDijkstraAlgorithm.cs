using System.Collections.Generic;
using Eocron.Algorithms.Queues;

namespace Eocron.Algorithms.Graphs
{
    /// <summary>
    ///     https://en.wikipedia.org/wiki/Dijkstra%27s_algorithm
    ///     This implementation is infinite one. This means its not necessary to know entire graph at source and you
    ///     can provide more data as you go deeper in graph.
    ///     Complexity: O(E + V*log(V))
    ///     Memory: O(V)
    /// </summary>
    /// <typeparam name="TVertex">Vertex type</typeparam>
    /// <typeparam name="TWeight">Weight type</typeparam>
    public class InfiniteDijkstraAlgorithm<TVertex, TWeight> : DijkstraAlgorithmBase<TVertex, TWeight>
    {
        private readonly IDictionary<TVertex, TVertex> _paths;
        private readonly IDictionary<TVertex, TWeight> _weights;

        public InfiniteDijkstraAlgorithm(
            GetAllEdges getEdges,
            GetVertexWeight getVertexWeight,
            GetEdgeWeight getEdgeWeight,
            IsTargetVertex isTargetVertex = null,
            IComparer<TWeight> comparer = null)
            : base(getEdges, getVertexWeight, getEdgeWeight, isTargetVertex, comparer)
        {
            _weights = new Dictionary<TVertex, TWeight>();
            _paths = new Dictionary<TVertex, TVertex>();
        }

        public override bool TryGetWeight(TVertex vertex, out TWeight weight)
        {
            return _weights.TryGetValue(vertex, out weight);
        }

        protected override void SetWeight(TVertex vertex, TWeight weight)
        {
            _weights[vertex] = weight;
        }

        protected override void SetPath(TVertex vertex, TVertex other)
        {
            _paths[vertex] = other;
        }

        protected override bool ContainsPath(TVertex vertex)
        {
            return _paths.ContainsKey(vertex);
        }

        protected override void Clear()
        {
            _paths.Clear();
            _weights.Clear();
            base.Clear();
        }

        protected override IPriorityQueue<TWeight, TVertex> CreateQueue(IComparer<TWeight> comparer)
        {
            return new FibonacciHeap<TWeight, TVertex>(comparer);
        }

        protected override bool TryGetPath(TVertex source, out TVertex target)
        {
            return _paths.TryGetValue(source, out target);
        }

        protected override bool ContainsWeight(TVertex vertex)
        {
            return _weights.ContainsKey(vertex);
        }
    }
}