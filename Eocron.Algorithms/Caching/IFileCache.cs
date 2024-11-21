using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.Caching
{
    public interface IFileCache
    {
        Task<bool> ContainsKeyAsync(string key, CancellationToken ct = default);

        Task<IFileCacheLink> GetOrAddFileAsync(string key, FilePathProviderDelegate filePathProvider,
            CancellationToken ct = default);

        Task<IFileCacheLink> GetOrAddFileAsync(string key, string fileName, FilePathProviderDelegate filePathProvider,
            CancellationToken ct = default);

        Task<IFileCacheLink> GetOrAddStreamAsync(string key, StreamProviderDelegate streamProvider,
            CancellationToken ct = default);

        Task<IFileCacheLink> GetOrAddStreamAsync(string key, string fileName, StreamProviderDelegate streamProvider,
            CancellationToken ct = default);

        Task<bool> TryRemoveAsync(string key, CancellationToken ct = default);
    }
}