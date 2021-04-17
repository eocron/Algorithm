using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.FileCache
{
    /// <summary>
    /// Represents file system cache of files.
    /// Cache is working over some directory, and should be cleaned manually.
    /// To do so: invoke GarbageCollect method to give cache chance to cleanup.
    /// If you don't do this - cache will just grow over time.
    /// It will add 22 characters to base path. Be aware of your file paths.
    /// </summary>
    /// <typeparam name="TKey">Key of cache entry.</typeparam>
    public interface IFileCacheAsync<TKey>
    {
        /// <summary>
        /// Invalidates entire cache. It will created from scratch.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task InvalidateAsync(CancellationToken token);

        /// <summary>
        /// Invalidates single key in cache.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task InvalidateAsync(TKey key, CancellationToken token);

        /// <summary>
        /// Performs garbage collection. It can skip files which currently readed, so run it periodically.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task GarbageCollectAsync(CancellationToken token);

        /// <summary>
        /// Get stream from cache or add it.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="provider">Stream factory method. Stream will be closed and disposed on completion.</param>
        /// <param name="token"></param>
        /// <param name="policy">Caching policy to apply to cached item. If item expired it will be removed from cache at GC. Null if no policy needed and item invalidation performed by user.</param>
        /// <returns></returns>
        Task<Stream> GetStreamOrAddStreamAsync(TKey key, Func<TKey, Task<Stream>> provider, CancellationToken token, ICacheExpirationPolicy policy = null);

        /// <summary>
        /// Get stream from cache or add it by file in filesystem.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="provider">File path factory method.</param>
        /// <param name="token"></param>
        /// <param name="policy">Caching policy to apply to cached item. Null if no policy needed, and item invalidation performed by user.</param>
        /// <returns></returns>
        Task<Stream> GetStreamOrAddFileAsync(TKey key, Func<TKey, Task<string>> provider, CancellationToken token, ICacheExpirationPolicy policy = null);

        /// <summary>
        /// Add or update stream in cache.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="stream"></param>
        /// <param name="token"></param>
        /// <param name="policy">Caching policy to apply to cached item. Null if no policy needed, and item invalidation performed by user.</param>
        /// <param name="leaveOpen">Specified if provided stream should not be closed and disposed by cache.</param>
        /// <returns></returns>
        Task AddOrUpdateStreamAsync(TKey key, Stream stream, CancellationToken token, ICacheExpirationPolicy policy, bool leaveOpen = false);

        Task AddOrUpdateFileAsync(TKey key, string sourceFilePath, CancellationToken token, ICacheExpirationPolicy policy);

        Task GetFileOrAddStreamAsync(TKey key, Func<TKey, Task<Stream>> provider, CancellationToken token, string targetFilePath, ICacheExpirationPolicy policy);
        Task GetFileOrAddFileAsync(TKey key, Func<TKey, Task<string>> provider, CancellationToken token, string targetFilePath, ICacheExpirationPolicy policy);

        Task<bool> TryGetFileAsync(TKey key, CancellationToken token, string targetFilePath);

        Task<Stream> TryGetStreamAsync(TKey key, CancellationToken token);
    }
}
