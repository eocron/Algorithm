using System;
using System.Collections.Generic;
using System.Linq;
using Eocron.Algorithms.Queues;

namespace Eocron.Algorithms.Graphs
{
    /// <summary>
    /// https://en.wikipedia.org/wiki/Dijkstra%27s_algorithm
    /// This implementation is infinite one. This means its not necessary to know entire graph at source and you
    /// can provide more data as you go deeper in graph.
    /// This base class allow you to allocate any vertex/edge/weights containers you like.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type</typeparam>
    /// <typeparam name="TWeight">Weight type</typeparam>
    public abstract class DijkstraAlgorithmBase<TVertex, TWeight> : IDijkstraAlgorithm<TVertex, TWeight>
    {
        private readonly GetAllEdges _getEdges;
        private readonly GetVertexWeight _getVertexWeight;
        private readonly GetEdgeWeight _getEdgeWeight;
        private readonly IsTargetVertex _isTargetVertex;
        private readonly IComparer<TWeight> _comparer;

        public TVertex Source { get; private set; }
        public TVertex Target { get; private set; }
        public bool IsTargetFound { get; private set; }

        private bool _searched;

        /// <summary>
        /// Calculates edge weight.
        /// So for example (x,y)=> (x.Weight + 1) means that traveling from X to Y costs 1 plus some calculated weight.
        /// This particular example can be used to calculate shortest path where all edges are costed 1
        /// </summary>
        /// <param name="vertexAndItsCurrentWeight">Source vertex and its current calculated weight.</param>
        /// <param name="nextVertex">Target vertex.</param>
        /// <returns></returns>
        public delegate TWeight GetEdgeWeight(VertexWeight vertexAndItsCurrentWeight, TVertex nextVertex);

        /// <summary>
        /// Calculates vertex weight.
        /// </summary>
        /// <param name="vertex">Some vertex.</param>
        /// <returns></returns>
        public delegate TWeight GetVertexWeight(TVertex vertex);

        /// <summary>
        /// Returns all possible target verticies.
        /// </summary>
        /// <param name="vertex">Source vertex.</param>
        /// <returns></returns>
        public delegate IEnumerable<TVertex> GetAllEdges(TVertex vertex);

        /// <summary>
        /// Checks if vertex is final and algorithm can stop traveling infinite/finite graph.
        /// Basically searching behavior limiter.
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        public delegate bool IsTargetVertex(TVertex vertex);

        /// <summary>
        /// Perform search in graph.
        /// </summary>
        /// <param name="getEdges">Get all outgoing edges.</param>
        /// <param name="getVertexWeight">Get vertex weight.</param>
        /// <param name="getEdgeWeight">Get edge weight from X vertex to Y vertex.</param>
        /// <param name="isTargetVertex">Checks if target is found.</param>
        /// <param name="comparer">Weight comparer.</param>
        protected DijkstraAlgorithmBase(
            GetAllEdges getEdges,
            GetVertexWeight getVertexWeight,
            GetEdgeWeight getEdgeWeight,
            IsTargetVertex isTargetVertex = null,
            IComparer<TWeight> comparer = null)
        {
            _getEdges = getEdges;
            _getVertexWeight = getVertexWeight;
            _getEdgeWeight = getEdgeWeight;
            _isTargetVertex = isTargetVertex;
            _comparer = comparer ?? Comparer<TWeight>.Default;
        }

        public class VertexWeight
        {
            public TVertex Vertex { get; }
            public TWeight Weight { get; }

            internal VertexWeight(TWeight weight, TVertex vertex)
            {
                Weight = weight;
                Vertex = vertex;
            }
        }

        public void Search(TVertex source)
        {
            Clear();
            try
            {
                var queue = CreateQueue(_comparer);
                var w = _getVertexWeight(source);
                queue.Enqueue(new KeyValuePair<TWeight, TVertex>(w, source));
                SetWeight(source, w);

                while (queue.Count > 0)
                {
                    var u = queue.Dequeue().Value;

                    if (_isTargetVertex?.Invoke(u) ?? false)
                    {
                        Source = source;
                        Target = u;
                        IsTargetFound = true;
                        _searched = true;
                        return;
                    }

                    var neighbors = _getEdges(u);
                    if (neighbors == null)
                        continue;

                    foreach (var v in neighbors)
                    {
                        if (!TryGetWeight(u, out var uWeight))
                            continue; //u not initialized -> infinity in alt -> no reason to move on with this vertex comparison

                        var alternativeWeightOfV = _getEdgeWeight(new VertexWeight(uWeight, u), v);
                        if (!TryGetWeight(v, out var vWeight) ||
                            _comparer.Compare(alternativeWeightOfV, vWeight) <
                            0) //if v is not initialized (infinity) or v cost lower than alternative
                        {
                            SetWeight(v, alternativeWeightOfV);
                            SetPath(v, u);
                            var item = new KeyValuePair<TWeight, TVertex>(alternativeWeightOfV, v);
                            queue.EnqueueOrUpdate(item, x => item); //decrease or add priority
                        }
                    }
                }

                Source = source;
                _searched = true;
            }
            catch
            {
                Clear();
                throw;
            }
        }

        public IEnumerable<TVertex> GetPathFromSourceToTarget()
        {
            ThrowIfNotSearched();
            return IsTargetFound
                ? GetPath(Source, Target)
                : Enumerable.Empty<TVertex>();
        }

        public IEnumerable<TVertex> GetPath(
            TVertex source,
            TVertex target)
        {
            ThrowIfNotSearched();
            var u = target;
            var isReachable = ContainsPath(u) || source.Equals(u);
            if (!isReachable)
                return null;

            var stack = new Stack<TVertex>();
            while (ContainsWeight(u))
            {
                stack.Push(u);
                if (!TryGetPath(u, out u))
                {
                    break;
                }
            }

            return stack;
        }
        
        protected abstract bool TryGetWeight(TVertex vertex, out TWeight weight);

        protected abstract void SetWeight(TVertex vertex, TWeight weight);

        protected abstract void SetPath(TVertex vertex, TVertex other);

        protected virtual bool ContainsPath(TVertex vertex) => TryGetPath(vertex, out _);

        protected virtual void Clear()
        {
            Source = default;
            Target = default;
            IsTargetFound = default;
            _searched = false;
        }

        protected abstract IPriorityQueue<TWeight, TVertex> CreateQueue(IComparer<TWeight> comparer);

        protected abstract bool TryGetPath(TVertex source, out TVertex target);

        protected virtual bool ContainsWeight(TVertex vertex) => TryGetWeight(vertex, out var _);

        private void ThrowIfNotSearched()
        {
            if (!_searched)
                throw new InvalidOperationException("Perform search.");
        }
    }
}