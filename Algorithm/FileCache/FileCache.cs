using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Algorithm.FileCache
{
    public sealed class FileCache<TKey> : IFileCache<TKey>, IDisposable
    {
        #region Private helper classes
        private delegate Task CancellableAction(CancellationToken token);
        private sealed class PerKeySemaphoreSlim : IDisposable
        {
            private readonly int _initialCount;
            private readonly ConcurrentDictionary<TKey, Lazy<SemaphoreSlim>> _perKeyLock = new ConcurrentDictionary<TKey, Lazy<SemaphoreSlim>>();

            public PerKeySemaphoreSlim(int initialCount)
            {
                _initialCount = initialCount;
            }
            public async Task WaitAsync(TKey key, CancellationToken token)
            {
                var sema = _perKeyLock.GetOrAdd(key, x => new Lazy<SemaphoreSlim>(() => new SemaphoreSlim(_initialCount))).Value;
                await sema.WaitAsync(token);
            }

            public void ReleaseLock(TKey key)
            {
                Lazy<SemaphoreSlim> lazy;
                _perKeyLock.TryRemove(key, out lazy);
                if (lazy.IsValueCreated)
                    lazy.Value.Release();
            }

            public void Dispose()
            {
                foreach (var kv in _perKeyLock)
                {
                    var lazy = kv.Value;
                    if (lazy.IsValueCreated)
                    {
                        lazy.Value.Dispose();
                    }
                }
            }
        }
        private sealed class CFileCacheEntry : AnyExpirationPolicy
        {
            public string FilePath { get; set; }

            public DateTime Created { get; set; }
        }
        private sealed class CFileSource
        {
            private readonly int _uploadBufferSize = 81920;
            private readonly IFileSystem _fs;
            private string FilePath { get; set; }

            private Stream Stream { get; set; }

            public CFileSource(string filePath, IFileSystem fs)
            {
                _fs = fs;
                FilePath = filePath;
            }

            public CFileSource(Stream stream, IFileSystem fs)
            {
                _fs = fs;
                Stream = stream;
            }

            public async Task CopyToAsync(string path, CancellationToken token, bool createHardLink)
            {
                if (Stream != null)
                {
                    using (var fstream = await _fs.OpenCreateAsync(path, token))
                    {
                        await Stream.CopyToAsync(fstream, _uploadBufferSize, token);
                    }
                }
                else
                {
                    if (createHardLink)
                    {
                        await _fs.CreateHardLink(FilePath, path, token);
                    }
                    else
                    {
                        await _fs.CopyFileAsync(FilePath, path, token);
                    }
                }
            }
        }
        #endregion

        private static long _uniqueIdCounter        = 0;

        private readonly int _gcIntervalMs          = 5 * 1000;
        private readonly int _gcFailRetryIntervalMs = 10 * 1000;
        private readonly PerKeySemaphoreSlim                _perKeyLock;
        private readonly IFileSystem                        _fs;
        private readonly string                             _baseFolder;
        private readonly AsyncReaderWriterLock              _cacheLock;
        private readonly CancellationTokenSource            _cts;
        private readonly Task                               _gc;

        private volatile bool                               _invalid;
        private string                                      _currentFolder;
        private string                                      _tempFolder;
        private string                                      _cacheFolder;
        private string                                      _trashFolder;
        private ConcurrentDictionary<TKey, CFileCacheEntry> _entries;
        private ConcurrentBag<CancellableAction>            _actions;

        public string BaseFolder => _baseFolder;
        public string CurrentFolder
        {
            get
            {
                EnsureInitializedAsync(CancellationToken.None).GetAwaiter().GetResult();
                return _currentFolder;
            }
        }

        /// <summary>
        /// File cache default ctr.
        /// </summary>
        /// <param name="baseFolder">Folder in which this instance of file cache will reside.</param>
        /// <param name="fileSystem">File system interface to use in all file operation. Defaul is current file system.</param>
        /// <param name="disableGc">Check if you will manage garbage collection yourself.</param>
        public FileCache(string baseFolder, IFileSystem fileSystem = null, bool disableGc = false)
        {
            if (baseFolder == null)
                throw new ArgumentNullException(nameof(baseFolder));
            _perKeyLock = new PerKeySemaphoreSlim(1);
            _cacheLock = new AsyncReaderWriterLock();
            _fs = fileSystem ?? FileSystem.Instance;
            _baseFolder = baseFolder;
            _invalid = true;

            if (!disableGc)
            {
                _cts = new CancellationTokenSource();
                _gc = GcTask(_cts.Token);
            }
        }

        private async Task GcTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    bool failed = false;
                    try
                    {
                        await GarbageCollect(token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch //we swallow exceptions of other types.
                    {
                        failed = true;
                    }
                    token.ThrowIfCancellationRequested();
                    await Task.Delay(failed ? _gcFailRetryIntervalMs : _gcIntervalMs, token);
                }
                catch(OperationCanceledException)
                {
                    break;
                }
            }
        }


        #region Private methods
        private static string GetUniqueFileName()
        {
            //it is very unlikelly to iterate over entire uniqueId and come back to collision.
            var id = unchecked((ulong)Interlocked.Increment(ref _uniqueIdCounter));
            return id.ToString("X8");
        }

        /// <summary>
        /// Locks CurrentFolder to write so it can be modified in process.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<IDisposable> GlobalWriteLock(CancellationToken token)
        {
            return await _cacheLock.ReaderLockAsync(token);
        }

        /// <summary>
        /// Locks CurrentFolder to read so it can't be changed in process.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<IDisposable> GlobalReadLock(CancellationToken token)
        {
            return await _cacheLock.WriterLockAsync(token);
        }

        private async Task EnsureInitializedAsync(CancellationToken token)
        {
            if (!_invalid)
                return;
            using (await GlobalWriteLock(token))
            {
                if (!_invalid)
                    return;

                var currentFolder = await GenerateIncrementTmpPath(_baseFolder, token);
                var tempFolder = Path.Combine(currentFolder, "tmp");
                var cacheFolder = Path.Combine(currentFolder, "cch");
                var trashFolder = Path.Combine(currentFolder, "bin");
                var transactionLogPath = Path.Combine(currentFolder, "journal.txt");
                var entries = new ConcurrentDictionary<TKey, CFileCacheEntry>();
                var actions = new ConcurrentBag<CancellableAction>();
                token.ThrowIfCancellationRequested();

                _currentFolder = currentFolder;
                _tempFolder = tempFolder;
                _cacheFolder = cacheFolder;
                _trashFolder = trashFolder;
                _entries = entries;
                _actions = actions;
                _invalid = false;
            }
        }

        /// <summary>
        /// Enforces execution of particular method. It will be retried on each garbage collect.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task ShouldExecute(CancellableAction action, CancellationToken token)
        {
            try
            {
                await action(token);
                return;
            }
            catch { }
            _actions.Add(action);
        }

        private async Task ShouldExecuteIteration(CancellationToken token)
        {
            if (_actions.Any())
            {
                CancellableAction a;

                var toRetry = new List<CancellableAction>();
                while (_actions.TryTake(out a))
                {
                    try
                    {
                        await a(token);
                    }
                    catch
                    {
                        toRetry.Add(a);
                    }
                }

                foreach(var r in toRetry)
                {
                    _actions.Add(r);
                }
            }
        }
        

        private async Task Ensure(string dir, CancellationToken token)
        {
            await _fs.CreateDirectoryAsync(dir, token);
        }

        private async Task<string> GenerateIncrementTmpPath(string folder, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await Ensure(folder, token);
            int i = 0;
            do
            {
                token.ThrowIfCancellationRequested();

                var path = Path.Combine(folder, "fs" + GetUniqueFileName());
                if (!await _fs.FileExistAsync(path, token) && !await _fs.DirectoryExistAsync(path, token))
                    return path;

                i++;
            } while (true);
        }

        private async Task<string> GetTmpFileAsync(string folder, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await Ensure(folder, token);
            while (true)
            {
                token.ThrowIfCancellationRequested();

                var path = Path.Combine(folder, GetUniqueFileName());
                if (await _fs.FileExistAsync(path, token))
                    continue;

                return path;
            }
        }

        private async Task MoveToTrashAsync(string filePath, CancellationToken token)
        {
            await ShouldExecute(async (t) =>
            {
                await Ensure(_trashFolder, t);
                //new ZlpFileInfo(filePath).Attributes = FileAttributes.Normal;
                if (await _fs.FileExistAsync(filePath, token))
                {
                    var newPath = await GetTmpFileAsync(_trashFolder, t);
                    await _fs.MoveFileAsync(filePath, newPath, t);
                }
            }, token);
        }

        private async Task<string> MoveToCacheAsync(string filePath, CancellationToken token)
        {
            await Ensure(_cacheFolder, token);
            var newPath = await GetTmpFileAsync(_cacheFolder, token);
            await _fs.MoveFileAsync(filePath, newPath, token);
            //new ZlpFileInfo(newPath).Attributes |= FileAttributes.Readonly;
            return newPath;
        }

        private async Task<string> GetTmpUploadFilePath(CancellationToken token)
        {
            return await GetTmpFileAsync(_tempFolder, token);
        }

        private async Task<string> UploadToCacheAsync(CFileSource src, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var path = await GetTmpUploadFilePath(token);
            try
            {
                await src.CopyToAsync(path, token, false);
                token.ThrowIfCancellationRequested();
                return await MoveToCacheAsync(path, token);
            }
            catch
            {
                if (await _fs.FileExistAsync(path, token))
                    await MoveToTrashAsync(path, token);
                throw;
            }
        }

        private async Task InternalAddOrUpdate(TKey key, CFileSource src, CancellationToken token, ICacheExpirationPolicy policy)
        {
            var path = await UploadToCacheAsync(src, token);
            _entries[key] = new CFileCacheEntry()
            {
                Created = DateTime.UtcNow,
                FilePath = path
            }.Pulse(policy);
        }

        private async Task<CFileCacheEntry> InternalGetOrAdd(TKey key, Func<TKey, Task<CFileSource>> provider, CancellationToken token, ICacheExpirationPolicy policy)
        {
            CFileCacheEntry cacheEntry;

            if (_entries.TryGetValue(key, out cacheEntry))
                return cacheEntry.Pulse(policy);

            //per key mutex required to avoid multiple file upload to cache by same key.
            //files are very large objects, so unnecessary actions with them should be avoided if possible.
            await _perKeyLock.WaitAsync(key, token);
            try
            {
                if (_entries.TryGetValue(key, out cacheEntry))
                    return cacheEntry.Pulse(policy);

                //this section will be executed one time per key at single moment
                token.ThrowIfCancellationRequested();
                var src = await provider(key);
                token.ThrowIfCancellationRequested();
                var path = await UploadToCacheAsync(src, token);
                cacheEntry = new CFileCacheEntry()
                {
                    Created = DateTime.UtcNow,
                    FilePath = path
                };
                //######
                _entries[key] = cacheEntry;

                return cacheEntry.Pulse(policy);
            }
            finally
            {
                _perKeyLock.ReleaseLock(key);
            }
        }

        private Task<CFileCacheEntry> InternalGetEntry(TKey key, CancellationToken token)
        {
            CFileCacheEntry cacheEntry;
            if (_entries.TryGetValue(key, out cacheEntry))
                return Task.FromResult(cacheEntry.Pulse(null));
            return Task.FromResult((CFileCacheEntry)null);
        }


        private async Task SafeDeleteFile(string path, CancellationToken token)
        {
            try
            {
                await _fs.DeleteFileAsync(path, token);
            }
            catch
            {

            }
        }

        private async Task DeleteAsync(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (await _fs.FileExistAsync(path, token))
            {
                token.ThrowIfCancellationRequested();
                await SafeDeleteFile(path, token);
            }
            token.ThrowIfCancellationRequested();
            if (await _fs.DirectoryExistAsync(path, token))
            {
                token.ThrowIfCancellationRequested();
                foreach (var file in await _fs.GetFilesAsync(path, "*", SearchOption.TopDirectoryOnly, token))
                {
                    token.ThrowIfCancellationRequested();
                    await SafeDeleteFile(file, token);
                }
                token.ThrowIfCancellationRequested();
                foreach (var dir in await _fs.GetDirectoriesAsync(path, "*", SearchOption.TopDirectoryOnly, token))
                {
                    token.ThrowIfCancellationRequested();
                    await DeleteAsync(dir, token);
                }
                token.ThrowIfCancellationRequested();
                try
                {
                    await _fs.DeleteDirectoryNonRecursiveAsync(path, token);
                }
                catch { }
            }
        }

        #endregion

        public Task InvalidateAsync(CancellationToken token)
        {
            _invalid = true;
            return Task.CompletedTask;
        }

        public async Task InvalidateAsync(TKey key, CancellationToken token)
        {
            if (_invalid)//no meaning to invalidate if cache is entirely invalidated.
                return;

            using (await GlobalReadLock(token))
            {
                if (_invalid)
                    return;

                CFileCacheEntry entry;
                if (!_entries.TryRemove(key, out entry))
                    return;

                await MoveToTrashAsync(entry.FilePath, token);
            }
        }

        public async Task GarbageCollect(CancellationToken token)
        {
            var now = DateTime.UtcNow;
            using (await GlobalReadLock(token))
            {
                var nonCacheFolders = new List<string>();
                token.ThrowIfCancellationRequested();
                foreach ( var dir in await _fs.GetDirectoriesAsync(_baseFolder, "fs*", SearchOption.TopDirectoryOnly, token))
                {
                    token.ThrowIfCancellationRequested();
                    if (!await _fs.EqualsAsync(dir, _currentFolder, token))
                        nonCacheFolders.Add(dir);
                }

                if (nonCacheFolders != null && nonCacheFolders.Any())
                {
                    foreach (var dir in nonCacheFolders)
                    {
                        token.ThrowIfCancellationRequested();
                        await DeleteAsync(dir, token);
                    }
                }

                await ShouldExecuteIteration(token);

                token.ThrowIfCancellationRequested();
                if (_entries != null && _entries.Any())
                {
                    var expired = _entries.Where(x => x.Value.IsExpired(now)).Select(x => x.Key);
                    foreach (var key in expired)
                    {
                        token.ThrowIfCancellationRequested();
                        CFileCacheEntry entry;
                        if (_entries.TryRemove(key, out entry))
                        {
                            await MoveToTrashAsync(entry.FilePath, token);
                        }
                    }
                }
                token.ThrowIfCancellationRequested();
                if (_trashFolder != null && await _fs.DirectoryExistAsync(_trashFolder, token))
                {
                    token.ThrowIfCancellationRequested();
                    foreach (var file in await _fs.GetFilesAsync(_trashFolder, "*", SearchOption.TopDirectoryOnly, token))
                    {
                        token.ThrowIfCancellationRequested();
                        await DeleteAsync(file, token);
                    }
                }
            }
        }

        public async Task<Stream> GetStreamOrAddStreamAsync(TKey key, Func<TKey, Task<Stream>> provider, CancellationToken token, ICacheExpirationPolicy policy)
        {
            await EnsureInitializedAsync(token);
            using (await GlobalReadLock(token))
            {
                Func<TKey, Task<CFileSource>> op = async kk => new CFileSource(await provider(kk), _fs);
                var entry = await InternalGetOrAdd(key, op, token, policy);
                return await _fs.OpenReadAsync(entry.FilePath, token);
            }
        }

        public async Task<Stream> GetStreamAddFileAsync(TKey key, Func<TKey, Task<string>> provider, CancellationToken token, ICacheExpirationPolicy policy)
        {
            await EnsureInitializedAsync(token);
            using (await GlobalReadLock(token))
            {
                var entry = await InternalGetOrAdd(key, async kk => new CFileSource(await provider(kk), _fs), token, policy);
                return await _fs.OpenReadAsync(entry.FilePath, token);
            }
        }

        public async Task AddOrUpdateStreamAsync(TKey key, Stream stream, CancellationToken token, ICacheExpirationPolicy policy)
        {
            await EnsureInitializedAsync(token);
            using (await GlobalReadLock(token))
            {
                await InternalAddOrUpdate(key, new CFileSource(stream, _fs), token, policy);
            }
        }

        public async Task AddOrUpdateFileAsync(TKey key, string sourceFilePath, CancellationToken token, ICacheExpirationPolicy policy)
        {
            await EnsureInitializedAsync(token);
            using (await GlobalReadLock(token))
            {
                await InternalAddOrUpdate(key, new CFileSource(sourceFilePath, _fs), token, policy);
            }
        }

        public async Task GetFileOrAddStreamAsync(TKey key, Func<TKey, Task<Stream>> provider, CancellationToken token, string targetFilePath, ICacheExpirationPolicy policy)
        {
            await EnsureInitializedAsync(token);
            using (await GlobalReadLock(token))
            {
                var entry = await InternalGetOrAdd(key, async kk => new CFileSource(await provider(kk), _fs), token, policy);
                await new CFileSource(entry.FilePath, _fs).CopyToAsync(targetFilePath, token, true);
            }
        }

        public async Task GetFileOrAddFileAsync(TKey key, Func<TKey, Task<string>> provider, CancellationToken token, string targetFilePath, ICacheExpirationPolicy policy)
        {
            await EnsureInitializedAsync(token);
            using (await GlobalReadLock(token))
            {
                var entry = await InternalGetOrAdd(key, async kk => new CFileSource(await provider(kk), _fs), token, policy);
                await new CFileSource(entry.FilePath, _fs).CopyToAsync(targetFilePath, token, true);
            }
        }

        public async Task<bool> TryGetFile(TKey key, CancellationToken token, string targetFilePath)
        {
            await EnsureInitializedAsync(token);
            using (await GlobalReadLock(token))
            {
                var entry = await InternalGetEntry(key, token);
                if (entry == null)
                    return false;

                await new CFileSource(entry.FilePath, _fs).CopyToAsync(targetFilePath, token, true);
                return true;
            }
        }

        public async Task<Stream> TryGetStream(TKey key, CancellationToken token)
        {
            await EnsureInitializedAsync(token);
            using (await GlobalReadLock(token))
            {
                var entry = await InternalGetEntry(key, token);
                if (entry == null)
                    return null;

                return await _fs.OpenReadAsync(entry.FilePath, token);
            }
        }

        public void Dispose()
        {
            _cts?.Dispose();
            _gc?.Wait();
            _perKeyLock.Dispose();
            _cacheLock.Dispose();
        }
    }
}
