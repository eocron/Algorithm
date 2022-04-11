using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using Eocron.Algorithms.Queues;

namespace Eocron.Algorithms.Graphs
{
    /// <summary>
    /// https://en.wikipedia.org/wiki/Dijkstra%27s_algorithm
    /// This implementation is finite. Count of verticies should be know beforehand.
    /// Much faster than infinite one, because allocates memory beforehand.
    /// 
    /// Verticies index range: [0,V)
    /// Complexity: O(E + V*log(V))
    /// Memory: O(V)
    /// </summary>
    /// <typeparam name="TWeight">Weight type</typeparam>
    public class FiniteDijkstraAlgorithm<TWeight> : DijkstraAlgorithmBase<int, TWeight>, IDisposable
    {
        private readonly ArrayPool<Item> _pool;
        private readonly Item[] _items;

        private struct Item
        {
            public bool WeightInitialized;
            public TWeight Weight;
            public bool PathInitialized;
            public int Path;
        }

        public FiniteDijkstraAlgorithm(
            int vertexCount,
            GetAllEdges getEdges,
            GetVertexWeight getVertexWeight,
            GetEdgeWeight getEdgeWeight,
            IsTargetVertex isTargetVertex = null,
            IComparer<TWeight> comparer = null)
            : base(getEdges, getVertexWeight, getEdgeWeight, isTargetVertex, comparer)
        {
            _pool = ArrayPool<Item>.Shared;
            _items = _pool.Rent(vertexCount);
        }


        public override bool TryGetWeight(int vertex, out TWeight weight)
        {
            if (!_items[vertex].WeightInitialized)
            {
                weight = default;
                return false;
            }

            weight = _items[vertex].Weight;
            return true;
        }

        protected override void SetWeight(int vertex, TWeight weight)
        {
            _items[vertex].WeightInitialized = true;
            _items[vertex].Weight = weight;
        }

        protected override void SetPath(int vertex, int other)
        {
            _items[vertex].PathInitialized = true;
            _items[vertex].Path = other;
        }

        protected override IPriorityQueue<TWeight, int> CreateQueue(IComparer<TWeight> comparer)
        {
            return new FibonacciHeap<TWeight, int>(comparer);
        }

        protected override bool TryGetPath(int source, out int target)
        {
            if (!_items[source].PathInitialized)
            {
                target = default;
                return false;
            }

            target = _items[source].Path;
            return true;
        }

        protected override void Clear()
        {
            Array.Clear(_items, 0, _items.Length);
            base.Clear();
        }

        public void Dispose()
        {
            _pool.Return(_items);
        }
    }
}