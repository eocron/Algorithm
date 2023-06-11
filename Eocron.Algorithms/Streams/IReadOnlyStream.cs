using System.Collections.Generic;

namespace Eocron.Algorithms.Streams
{
    public interface IReadOnlyStream<out TChunk> : IAsyncEnumerable<TChunk>, IEnumerable<TChunk>
    {
    }
}
