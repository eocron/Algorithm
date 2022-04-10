using Eocron.Algorithms.Queues;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.Algorithms.Graphs
{
    /// <summary>
    /// https://en.wikipedia.org/wiki/Dijkstra%27s_algorithm
    /// This implementation is infinite one. This means its not necessary to know entire graph at source and you
    /// can provide more data as you go deeper in graph.
    /// Complexity is O(E + V*log(V)), where E number of edges and V number of verticies.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type</typeparam>
    /// <typeparam name="TWeight">Weight type</typeparam>
    public static class DijkstraAlgorithm<TVertex, TWeight>
    {
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

        public class SearchResult
        {
            public TVertex Source { get; set; }

            public TVertex Target { get; set; }

            public bool IsTargetFound { get; set; }

            /// <summary>
            /// All traveled vertex weights. If target condition specified, can contain partial weight matrix.
            /// </summary>
            public Dictionary<TVertex, TWeight> Weights { get; set; }

            /// <summary>
            /// Tree of cheapest paths.
            /// </summary>
            public Dictionary<TVertex, TVertex> ReversedPaths { get; set; }

            public IEnumerable<TVertex> PathFromSourceToTarget => IsTargetFound
                ? GetPath(Source, Target)
                : Enumerable.Empty<TVertex>();

            public IEnumerable<TVertex> GetPath(TVertex source, TVertex target)
            {
                return GetPath(ReversedPaths, Weights, source, target) ?? Enumerable.Empty<TVertex>();
            }
            private static IEnumerable<TVertex> GetPath(
                Dictionary<TVertex, TVertex> paths,
                Dictionary<TVertex, TWeight> weights,
                TVertex source,
                TVertex target)
            {

                if (paths.Count == 0)
                    return null;

                var u = target;
                var isReachable = paths.ContainsKey(u) || source.Equals(u);
                if (!isReachable)
                    return null;

                var stack = new Stack<TVertex>();
                while (weights.ContainsKey(u))
                {
                    stack.Push(u);
                    if (!paths.TryGetValue(u, out u))
                    {
                        break;
                    }
                }

                return stack;
            }
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

        /// <summary>
        /// Perform search in graph.
        /// </summary>
        /// <param name="source">Source vertex.</param>
        /// <param name="getEdges">Get all outgoing edges.</param>
        /// <param name="getEdgeWeight">Get edge weight from X vertex to Y vertex.</param>
        /// <param name="isTargetVertex">Checks if target is found.</param>
        /// <param name="comparer">Weight comparer.</param>
        /// <returns></returns>
        public static SearchResult Search(
            TVertex source,
            GetAllEdges getEdges,
            GetEdgeWeight getEdgeWeight,
            IsTargetVertex isTargetVertex = null,
            IComparer<TWeight> comparer = null)
        {
            return Search(
                source,
                getEdges,
                vertex => getEdgeWeight(new VertexWeight(default, vertex), vertex),
                getEdgeWeight,
                isTargetVertex,
                comparer);
        }

        /// <summary>
        /// Perform search in graph.
        /// </summary>
        /// <param name="source">Source vertex.</param>
        /// <param name="getEdges">Get all outgoing edges.</param>
        /// <param name="getVertexWeight">Get vertex weight.</param>
        /// <param name="getEdgeWeight">Get edge weight from X vertex to Y vertex.</param>
        /// <param name="isTargetVertex">Checks if target is found.</param>
        /// <param name="comparer">Weight comparer.</param>
        /// <returns></returns>
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
                        Source = source,
                        Target = u,
                        Weights = weights,
                        ReversedPaths = paths,
                        IsTargetFound = true
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
                Source = source,
                Weights = weights,
                ReversedPaths = paths
            };
        }



        private static IPriorityQueue<TWeight, TVertex> CreateQueue(IComparer<TWeight> comparer)
        {
            return new FibonacciHeap<TWeight, TVertex>(comparer);
        }
    }
}
