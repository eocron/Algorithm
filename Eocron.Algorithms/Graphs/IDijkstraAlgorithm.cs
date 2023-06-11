using System.Collections.Generic;

namespace Eocron.Algorithms.Graphs
{
    public interface IDijkstraAlgorithm<TVertex, TWeight>
    {
        IEnumerable<TVertex> GetPath(TVertex source, TVertex target);

        IEnumerable<TVertex> GetPathFromSourceToTarget();

        public TWeight GetWeight(TVertex vertex);

        /// <summary>
        ///     Performs search on this particular algorithm, filling its weight/path matrix.
        /// </summary>
        /// <param name="source">Starting/source vertex.</param>
        void Search(TVertex source);

        public bool TryGetWeight(TVertex target, out TWeight weight);

        bool IsTargetFound { get; }
        TVertex Source { get; }
        TVertex Target { get; }
    }
}