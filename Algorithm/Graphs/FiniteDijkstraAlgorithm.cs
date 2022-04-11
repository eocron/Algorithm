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
        private readonly ArrayPool<byte> _bytePool;
        private readonly ArrayPool<TWeight> _weightMemoryPool;
        private readonly ArrayPool<int> _vertexMemoryPool;
        public TWeight[] Weights;
        public int[] Paths;
        public BitArray WeightsInitialized;
        public BitArray PathsInitialized;
        private readonly byte[] _weightsBitArray;
        private readonly byte[] _pathsBitArray;

        public FiniteDijkstraAlgorithm(
            int vertexCount,
            GetAllEdges getEdges,
            GetVertexWeight getVertexWeight,
            GetEdgeWeight getEdgeWeight,
            IsTargetVertex isTargetVertex = null,
            IComparer<TWeight> comparer = null,
            ArrayPool<TWeight> weightMemoryPool = null,
            ArrayPool<int> vertexMemoryPool = null,
            ArrayPool<byte> bytePool = null)
            : base(getEdges, getVertexWeight, getEdgeWeight, isTargetVertex, comparer)
        {
            _bytePool = bytePool ?? ArrayPool<byte>.Shared;
            _weightMemoryPool = weightMemoryPool ?? ArrayPool<TWeight>.Shared;
            _vertexMemoryPool = vertexMemoryPool ?? ArrayPool<int>.Shared;

            Weights = _weightMemoryPool.Rent(vertexCount);
            Paths = _vertexMemoryPool.Rent(vertexCount);
            var byteCount = vertexCount / sizeof(byte) + (vertexCount % sizeof(byte) == 0 ? 0 : 1);
            _weightsBitArray = _bytePool.Rent(byteCount);
            _pathsBitArray = _bytePool.Rent(byteCount);
            WeightsInitialized = new BitArray(_weightsBitArray);
            PathsInitialized = new BitArray(_pathsBitArray);
        }


        protected override bool TryGetWeight(int vertex, out TWeight weight)
        {
            if (!WeightsInitialized[vertex])
            {
                weight = default;
                return false;
            }

            weight = Weights[vertex];
            return true;
        }

        protected override void SetWeight(int vertex, TWeight weight)
        {
            WeightsInitialized[vertex] = true;
            Weights[vertex] = weight;
        }

        protected override void SetPath(int vertex, int other)
        {
            PathsInitialized[vertex] = true;
            Paths[vertex] = other;
        }

        protected override IPriorityQueue<TWeight, int> CreateQueue(IComparer<TWeight> comparer)
        {
            return new FibonacciHeap<TWeight, int>(comparer);
        }

        protected override bool TryGetPath(int source, out int target)
        {
            if (!PathsInitialized[source])
            {
                target = default;
                return false;
            }

            target = Paths[source];
            return true;
        }

        protected override void Clear()
        {
            PathsInitialized.SetAll(false);
            WeightsInitialized.SetAll(false);
            Array.Clear(Paths, 0, Paths.Length);
            Array.Clear(Weights, 0, Weights.Length);

            base.Clear();
        }

        public void Dispose()
        {
            _vertexMemoryPool.Return(Paths);
            _weightMemoryPool.Return(Weights);
            _bytePool.Return(_weightsBitArray);
            _bytePool.Return(_pathsBitArray);
        }
    }
}