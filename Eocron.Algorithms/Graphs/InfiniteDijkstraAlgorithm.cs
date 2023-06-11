using System;
using System.Collections.Generic;
using Eocron.Algorithms.Queues;

namespace Eocron.Algorithms.Graphs
{
    /// <summary>
    ///     https://en.wikipedia.org/wiki/Dijkstra%27s_algorithm
    ///     This implementation is infinite one. This means its not necessary to know entire graph at source and you
    ///     can provide more data as you go deeper in graph.
    ///     It will always find shortest path in VISITED nodes. So to find shortest out of shortest path you actually need to
    ///     visit ALL nodes.
    ///     Complexity: O(E + V*log(V))
    ///     Memory: O(V)
    /// </summary>
    /// <typeparam name="TVertex">Vertex type</typeparam>
    /// <typeparam name="TWeight">Weight type</typeparam>
    public sealed class InfiniteDijkstraAlgorithm<TVertex, TWeight> : DijkstraAlgorithmBase<TVertex, TWeight>
    {
        /// <summary>
        /// </summary>
        /// <param name="getEdges">Get all outgoing edges.</param>
        /// <param name="getVertexWeight">Get vertex weight.</param>
        /// <param name="getEdgeWeight">Get edge weight from X vertex to Y vertex.</param>
        /// <param name="isTargetVertex">Checks if target is found.</param>
        /// <param name="weightComparer">Weight comparer.</param>
        /// <param name="vertexEqualityComparer">Vertex equality comparer.</param>
        /// <param name="count">Count of verticies, if known</param>
        /// <param name="buildShortestPathTree">Should algorithm find all shortests paths</param>
        public InfiniteDijkstraAlgorithm(
            GetAllEdges getEdges,
            GetVertexWeight getVertexWeight,
            GetEdgeWeight getEdgeWeight,
            IsTargetVertex isTargetVertex = null,
            IComparer<TWeight> weightComparer = null,
            IEqualityComparer<TVertex> vertexEqualityComparer = null,
            int count = 0,
            bool buildShortestPathTree = false)
            : base(getEdges, getVertexWeight, getEdgeWeight, isTargetVertex, weightComparer)
        {
            _searchAll = buildShortestPathTree;
            vertexEqualityComparer ??= EqualityComparer<TVertex>.Default;
            _priorityQueue = new FibonacciHeap<TWeight, TVertex>(weightComparer);
            _weights = count <= 0
                ? new Dictionary<TVertex, TWeight>(vertexEqualityComparer)
                : new Dictionary<TVertex, TWeight>(count, vertexEqualityComparer);
            _paths = count <= 0
                ? new Dictionary<TVertex, TVertex>(vertexEqualityComparer)
                : new Dictionary<TVertex, TVertex>(count, vertexEqualityComparer);
        }

        public override bool TryGetWeight(TVertex vertex, out TWeight weight)
        {
            return _weights.TryGetValue(vertex, out weight);
        }

        protected override void Clear()
        {
            _paths.Clear();
            _weights.Clear();
            _priorityQueue?.Clear();
            base.Clear();
        }

        protected override bool ContainsPath(TVertex vertex)
        {
            return _paths.ContainsKey(vertex);
        }

        protected override bool ContainsWeight(TVertex vertex)
        {
            return _weights.ContainsKey(vertex);
        }

        protected override KeyValuePair<TWeight, TVertex> Dequeue()
        {
            return _priorityQueue.Dequeue();
        }

        protected override void Enqueue(KeyValuePair<TWeight, TVertex> item)
        {
            _priorityQueue.Enqueue(item);
        }

        protected override void EnqueueOrUpdate(KeyValuePair<TWeight, TVertex> item,
            Func<KeyValuePair<TWeight, TVertex>, KeyValuePair<TWeight, TVertex>> onUpdate)
        {
            if (_searchAll)
                _priorityQueue.Enqueue(item);
            else
                _priorityQueue.EnqueueOrUpdate(item, onUpdate);
        }

        protected override bool IsQueueEmpty()
        {
            return _priorityQueue.Count == 0;
        }

        protected override void SetPath(TVertex vertex, TVertex other)
        {
            _paths[vertex] = other;
        }

        protected override void SetWeight(TVertex vertex, TWeight weight)
        {
            _weights[vertex] = weight;
        }

        protected override bool TryGetPath(TVertex source, out TVertex target)
        {
            return _paths.TryGetValue(source, out target);
        }

        private readonly bool _searchAll;
        private readonly IDictionary<TVertex, TVertex> _paths;
        private readonly IDictionary<TVertex, TWeight> _weights;
        private readonly IPriorityQueue<TWeight, TVertex> _priorityQueue;
    }
}