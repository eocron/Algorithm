using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.IO.Caching
{
    public interface IFileCacheLockProvider
    {
        Task<IAsyncDisposable> LockReadAsync(string key, CancellationToken ct);

        Task<IAsyncDisposable> LockWriteAsync(string key, CancellationToken ct);
    }
}