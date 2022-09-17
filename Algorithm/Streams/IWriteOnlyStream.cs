using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.Streams
{
    public interface IWriteOnlyStream<in TChunk> : IDisposable, IAsyncDisposable
    {
        Task WriteAsync(TChunk chunk, CancellationToken ct = default);

        void Write(TChunk chunk);
    }
}