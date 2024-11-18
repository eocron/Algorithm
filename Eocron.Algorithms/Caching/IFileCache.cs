using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.Caching
{
    public interface IFileCache
    {
        Task<Stream> GetOrAddAsync(string key, Func<string, CancellationToken, Task<Stream>> streamProvider, CancellationToken ct = default);

        Task<Stream> TryGetAsync(string key, CancellationToken ct = default);

        Task<bool> TryRemoveAsync(string key, CancellationToken ct = default);
    }
}