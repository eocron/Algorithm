using System;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.Algorithms.Graphs
{
    public abstract class DijkstraAlgorithmBase<TVertex, TWeight> : IDijkstraAlgorithm<TVertex, TWeight>
    {
        /// <summary>
        ///     Returns all possible target verticies.
        /// </summary>
        /// <param name="vertex">Source vertex.</param>
        /// <returns></returns>
        public delegate IEnumerable<TVertex> GetAllEdges(TVertex vertex);

        /// <summary>
        ///     Calculates edge weight.
        ///     So for example (x,y)=> (x.Weight + 1) means that traveling from X to Y costs 1 plus some calculated weight.
        ///     This particular example can be used to calculate shortest path where all edges are costed 1
        /// </summary>
        /// <param name="vertexAndItsCurrentWeight">Source vertex and its current calculated weight.</param>
        /// <param name="nextVertex">Target vertex.</param>
        /// <returns></returns>
        public delegate TWeight GetEdgeWeight(VertexWeight vertexAndItsCurrentWeight, TVertex nextVertex);

        /// <summary>
        ///     Calculates vertex weight.
        /// </summary>
        /// <param name="vertex">Some vertex.</param>
        /// <returns></returns>
        public delegate TWeight GetVertexWeight(TVertex vertex);

        /// <summary>
        ///     Checks if vertex is final and algorithm can stop traveling infinite/finite graph.
        ///     Basically searching behavior limiter.
        /// </summary>
        /// <returns></returns>
        public delegate bool IsTargetVertex(VertexWeight vertexAndItsCurrentWeight);

        private readonly IComparer<TWeight> _comparer;
        private readonly GetAllEdges _getEdges;
        private readonly GetEdgeWeight _getEdgeWeight;
        private readonly GetVertexWeight _getVertexWeight;
        private readonly IsTargetVertex _isTargetVertex;

        private bool _searched;

        protected DijkstraAlgorithmBase(
            GetAllEdges getEdges,
            GetVertexWeight getVertexWeight,
            GetEdgeWeight getEdgeWeight,
            IsTargetVertex isTargetVertex,
            IComparer<TWeight> comparer)
        {
            _getEdges = getEdges ?? throw new ArgumentNullException(nameof(getEdges));
            _getVertexWeight = getVertexWeight ?? throw new ArgumentNullException(nameof(getVertexWeight));
            _getEdgeWeight = getEdgeWeight ?? throw new ArgumentNullException(nameof(getEdgeWeight));
            _isTargetVertex = isTargetVertex;
            _comparer = comparer ?? Comparer<TWeight>.Default;
        }

        public TVertex Source { get; private set; }
        public TVertex Target { get; private set; }
        public bool IsTargetFound { get; private set; }

        public void Search(TVertex source)
        {
            Clear();
            try
            {
                Source = source;
                var w = _getVertexWeight(source);
                Enqueue(new KeyValuePair<TWeight, TVertex>(w, source));
                SetWeight(source, w);

                while (!IsQueueEmpty())
                {
                    var wu = Dequeue();
                    var u = new VertexWeight(wu.Key, wu.Value);
                    if (_isTargetVertex?.Invoke(u) ?? false)
                    {
                        Target = u.Vertex;
                        IsTargetFound = true;
                        _searched = true;
                        return;
                    }

                    var neighbors = _getEdges(u.Vertex);
                    if (neighbors == null)
                        continue;

                    foreach (var v in neighbors)
                    {
                        var alternativeWeightOfV = _getEdgeWeight(u, v);
                        if (!TryGetWeight(v, out var vWeight) ||
                            _comparer.Compare(alternativeWeightOfV, vWeight) <
                            0) //if v is not initialized (infinity) or v cost lower than alternative
                        {
                            SetWeight(v, alternativeWeightOfV);
                            SetPath(v, u.Vertex);
                            var item = new KeyValuePair<TWeight, TVertex>(alternativeWeightOfV, v);
                            EnqueueOrUpdate(item, x => item); //decrease or add priority
                        }
                    }
                }
                
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
                return Enumerable.Empty<TVertex>();

            var stack = new Stack<TVertex>();
            while (ContainsWeight(u))
            {
                stack.Push(u);
                if (!TryGetPath(u, out u)) break;
            }

            return stack;
        }

        public abstract bool TryGetWeight(TVertex vertex, out TWeight weight);

        public TWeight GetWeight(TVertex vertex)
        {
            ThrowIfNotSearched();
            if (TryGetWeight(vertex, out var tmp))
                return tmp;
            throw new KeyNotFoundException(vertex.ToString());
        }

        protected abstract void SetWeight(TVertex vertex, TWeight weight);

        protected abstract void SetPath(TVertex vertex, TVertex other);

        protected abstract bool ContainsPath(TVertex vertex);

        protected virtual void Clear()
        {
            Source = default;
            Target = default;
            IsTargetFound = default;
            _searched = false;
        }

        protected abstract void Enqueue(KeyValuePair<TWeight, TVertex> item);

        protected abstract void EnqueueOrUpdate(KeyValuePair<TWeight, TVertex> item,
            Func<KeyValuePair<TWeight, TVertex>, KeyValuePair<TWeight, TVertex>> onUpdate);

        protected abstract KeyValuePair<TWeight, TVertex> Dequeue();

        protected abstract bool IsQueueEmpty();

        protected abstract bool TryGetPath(TVertex source, out TVertex target);

        protected abstract bool ContainsWeight(TVertex vertex);

        protected void ThrowIfNotSearched()
        {
            if (!_searched)
                throw new InvalidOperationException("Perform search.");
        }

        public class VertexWeight
        {
            internal VertexWeight(TWeight weight, TVertex vertex)
            {
                Weight = weight;
                Vertex = vertex;
            }

            public TVertex Vertex { get; }
            public TWeight Weight { get; }
        }
    }
}