using Eocron.Algorithms.Queues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Eocron.Algorithms.Graphs
{

    /// <summary>
    /// https://en.wikipedia.org/wiki/Dijkstra%27s_algorithm
    /// This implementation is infinite one. This means its not necessary to know entire graph at source and you
    /// can provide more data as you go deeper in graph.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type</typeparam>
    /// <typeparam name="TWeight">Weight type</typeparam>
    public static class DijkstraAlgorithm<TVertex, TWeight>
    {
        public delegate TWeight GetEdgeWeight(VertexWeight vertexAndItsCurrentWeight, TVertex nextVertex);

        public delegate TWeight GetVertexWeight(TVertex vertex);

        public delegate IEnumerable<TVertex> GetAllEdges(TVertex vertex);

        public delegate bool IsTargetVertex(TVertex vertex);

        public class SearchResult
        {
            public Dictionary<TVertex, TWeight> Weights { get; set; }
            public List<TVertex> PathToTarget { get; set; }
            public Dictionary<TVertex, TVertex> ReversedPaths { get; set; }
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

        public static SearchResult Search(
            TVertex start,
            GetAllEdges getEdges,
            GetEdgeWeight getEdgeWeight,
            IsTargetVertex isTargetVertex = null,
            IComparer<TWeight> comparer = null)
        {
            return Search(start,
                getEdges,
                vertex => getEdgeWeight(new VertexWeight(default, vertex), vertex),
                getEdgeWeight,
                isTargetVertex,
                comparer);
        }

        public static SearchResult Search(
            TVertex source,
            GetAllEdges getEdges,
            GetVertexWeight getVertexWeight,
            GetEdgeWeight getEdgeWeight,
            IsTargetVertex isTargetVertex = null,
            IComparer<TWeight> comparer = null)
        {
            comparer ??= Comparer<TWeight>.Default;
            var weights = new Dictionary<TVertex, TWeight>();
            var paths = new Dictionary<TVertex, TVertex>();
            var queue = CreateQueue(comparer);
            var w = getVertexWeight(source);
            queue.Enqueue(new KeyValuePair<TWeight, TVertex>(w, source));
            weights[source] = w;

            while (queue.Count > 0)
            {
                var u = queue.Dequeue().Value;

                if (isTargetVertex?.Invoke(u) ?? false)
                {
                    return new SearchResult
                    {
                        Weights = weights,
                        PathToTarget = GetPath(paths, weights, source, u).ToList(),
                        ReversedPaths = paths
                    };
                }

                var neighbors = getEdges(u);
                if(neighbors == null)
                    break;

                foreach (var v in neighbors)
                {
                    if (!weights.TryGetValue(u, out var uWeight))
                        continue;//u not initialized -> infinity in alt -> no reason to move on with this vertex comparison
                    
                    var alternativeWeightOfV = getEdgeWeight(new VertexWeight(uWeight, u), v);
                    if (!weights.TryGetValue(v, out var vWeight) || comparer.Compare(alternativeWeightOfV, vWeight) < 0)//if v is not initialized (infinity) or v cost lower than alternative
                    {
                        weights[v] = alternativeWeightOfV;
                        paths[v] = u;
                        var item = new KeyValuePair<TWeight, TVertex>(alternativeWeightOfV, v);
                        queue.EnqueueOrUpdate(item, x => item);//decrease or add priority
                    }
                }
            }

            return new SearchResult
            {
                Weights = weights,
                ReversedPaths = paths,
                PathToTarget = new List<TVertex>()
            };
        }

        private static IEnumerable<TVertex> GetPath(
            Dictionary<TVertex, TVertex> paths, 
            Dictionary<TVertex, TWeight> weights,
            TVertex source, 
            TVertex target)
        {

            if (paths.Count == 0)
                yield break;

            var u = target;
            var isReachable = paths.ContainsKey(u) || source.Equals(u);
            if (!isReachable)
                yield break;

            var stack = new Stack<TVertex>();
            while (weights.ContainsKey(u))
            {
                stack.Push(u);
                u = paths[u];
            }
        }

        private static IPriorityQueue<TWeight, TVertex> CreateQueue(IComparer<TWeight> comparer)
        {
            return new FibonacciHeap<TWeight, TVertex>(comparer);
        }
    }
}
