using Eocron.Algorithms.Queues;
using System.Collections.Generic;

namespace Eocron.Algorithms.Graphs
{
    /// <summary>
    /// https://en.wikipedia.org/wiki/Dijkstra%27s_algorithm
    /// This implementation is infinite one. This means its not necessary to know entire graph at source and you
    /// can provide more data as you go deeper in graph.
    /// 
    /// Complexity: O(E + V*log(V))
    /// Memory: O(V)
    /// </summary>
    /// <typeparam name="TVertex">Vertex type</typeparam>
    /// <typeparam name="TWeight">Weight type</typeparam>
    public class InfiniteDijkstraAlgorithm<TVertex, TWeight> : DijkstraAlgorithmBase<TVertex, TWeight>
    {
        public IDictionary<TVertex, TWeight> Weights;
        public IDictionary<TVertex, TVertex> Paths;
        public InfiniteDijkstraAlgorithm(
            GetAllEdges getEdges, 
            GetVertexWeight getVertexWeight, 
            GetEdgeWeight getEdgeWeight,
            IsTargetVertex isTargetVertex = null, 
            IComparer<TWeight> comparer = null) 
            : base(getEdges, getVertexWeight, getEdgeWeight, isTargetVertex, comparer)
        {
            Weights = new Dictionary<TVertex, TWeight>();
            Paths = new Dictionary<TVertex, TVertex>();
        }

        protected override bool TryGetWeight(TVertex vertex, out TWeight weight)
        {
            return Weights.TryGetValue(vertex, out weight);
        }

        protected override void SetWeight(TVertex vertex, TWeight weight)
        {
            Weights[vertex] = weight;
        }

        protected override void SetPath(TVertex vertex, TVertex other)
        {
            Paths[vertex] = other;
        }

        protected override bool ContainsPath(TVertex vertex)
        {
            return Paths.ContainsKey(vertex);
        }

        protected override void Clear()
        {
            Paths.Clear();
            Weights.Clear();
            base.Clear();
        }

        protected override IPriorityQueue<TWeight, TVertex> CreateQueue(IComparer<TWeight> comparer)
        {
            return new FibonacciHeap<TWeight, TVertex>(comparer);
        }

        protected override bool TryGetPath(TVertex source, out TVertex target)
        {
            return Paths.TryGetValue(source, out target);
        }

        protected override bool ContainsWeight(TVertex vertex)
        {
            return Weights.ContainsKey(vertex);
        }
    }
}
