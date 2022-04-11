using System.Collections.Generic;

namespace Eocron.Algorithms.Graphs
{
    public interface IDijkstraAlgorithm<TVertex, TWeight>
    {
        TVertex Source { get; }
        TVertex Target { get; }

        bool IsTargetFound { get; }

        /// <summary>
        /// Performs search on this particular algorithm, filling its weight/path matrix.
        /// </summary>
        /// <param name="source">Starting/source vertex.</param>
        void Search(TVertex source);

        IEnumerable<TVertex> GetPathFromSourceToTarget();

        IEnumerable<TVertex> GetPath(
            TVertex source,
            TVertex target);

        public bool TryGetWeight(TVertex target, out TWeight weight);

        public TWeight GetWeight(TVertex vertex);
    }
}