using Eocron.Algorithms.Disposing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.FileCache
{
    public sealed class FileCacheAsync<TKey> : IFileCacheAsync<TKey>, IDisposable
    {
        #region Private helper classes
        private delegate Task CancellableAction(CancellationToken token);

        private sealed class PerKeySemaphoreSlim : IDisposable
        {
            private sealed class RefCounted<T>
            {
                public RefCounted(T value)
                {
                    RefCount = 1;
                    Value = value;
                }

                public int RefCount { get; set; }
                public T Value { get; private set; }
            }

            private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
            private readonly Dictionary<object, RefCounted<SemaphoreSlim>> _dict = new Dictionary<object, RefCounted<SemaphoreSlim>>();

            private async Task<RefCounted<SemaphoreSlim>> GetOrCreate(object key, CancellationToken token)
            {
                RefCounted<SemaphoreSlim> item;
                await _lock.WaitAsync(token);
                try
                {
                    if (_dict.TryGetValue(key, out item))
                    {
                        ++item.RefCount;
                    }
                    else
                    {
                        item = new RefCounted<SemaphoreSlim>(new SemaphoreSlim(1, 1));
                        _dict[key] = item;
                    }
                }
                finally
                {
                    _lock.Release();
                }

                return item;
            }

            public async Task<IDisposable> LockAsync(object key, CancellationToken token)
            {
                var item = await GetOrCreate(key, token);
                await item.Value.WaitAsync(CancellationToken.None);
                return new Disposable(() =>
                {
                    _lock.Wait(CancellationToken.None);
                    try
                    {
                        --item.RefCount;
                        if (item.RefCount == 0)
                            _dict.Remove(key);
                    }
                    finally
                    {
                        _lock.Release();
                    }

                    item.Value.Release();
                });
            }

            public void Dispose()
            {
                _lock.Dispose();
                foreach (var v in _dict.Values)
                {
                    v.Value.Dispose();
                }
            }
        }

        private sealed class CFileCacheEntry : AnyExpirationPolicy
        {
            public string FilePath { get; set; }

            public DateTime Created { get; set; }
        }
        private sealed class CFileSource : IDisposable
        {
            private readonly int _uploadBufferSize = 81920;
            private readonly bool _leaveOpen;
            private readonly IFileSystemAsync _fs;
            private string FilePath { get; set; }

            private Stream Stream { get; set; }

            public CFileSource(string filePath, IFileSystemAsync fs)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    throw new ArgumentNullException("Source path is null or empty.");
                _fs = fs;
                FilePath = filePath;
            }

            public CFileSource(Stream stream, bool leaveOpen, IFileSystemAsync fs)
            {
                if (stream == null)
                    throw new ArgumentNullException("Stream is null.");
                _leaveOpen = leaveOpen;
                _fs = fs;
                Stream = stream;
            }

            public async Task CopyToAsync(string path, CancellationToken token, bool createHardLink)
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentNullException("Target path is null or empty.");

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
                        await _fs.CreateHardLinkAsync(FilePath, path, token);
                    }
                    else
                    {
                        await _fs.CopyFileAsync(FilePath, path, token);
                    }
                }
                await _fs.SetAttributesAsync(path, FileAttributes.Normal, token);
            }

            public void Dispose()
            {
                if (!_leaveOpen && Stream != null)
                {
                    try
                    {
                        Stream.Close();
                    }
                    catch { }
                    try
                    {
                        Stream.Dispose();
                    }
                    catch { }
                }
            }
        }
        #endregion

        private static long _uniqueIdCounter = 0;

        private readonly int _gcIntervalMs = 5 * 1000;
        private readonly int _gcFailRetryIntervalMs = 10 * 1000;
        private readonly PerKeySemaphoreSlim _perKeyLock;
        private readonly IFileSystemAsync _fs;
        private readonly string _baseFolder;
        private readonly AsyncReaderWriterLock _cacheLock;
        private readonly CancellationTokenSource _cts;
        private readonly Task _gc;

        private volatile bool _invalid;
        private string _currentFolder;
        private string _tempFolder;
        private string _cacheFolder;
        private string _trashFolder;
        private ConcurrentDictionary<TKey, CFileCacheEntry> _entries;
        private ConcurrentBag<CancellableAction> _actions;

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
        public FileCacheAsync(string baseFolder, IFileSystemAsync fileSystem = null, bool disableGc = false)
        {
            if (baseFolder == null)
                throw new ArgumentNullException(nameof(baseFolder));
            _perKeyLock = new PerKeySemaphoreSlim();
            _cacheLock = new AsyncReaderWriterLock();
            _fs = fileSystem ?? FileSystemAsync.Instance;
            _baseFolder = baseFolder;
            _invalid = true;

            if (!disableGc)
            {
                _cts = new CancellationTokenSource();
                _gc = GcTask(_cts.Token);
            }
        }

        private void NotNull(object obj, string name)
        {
            var str = obj as string;
            if (str != null && string.IsNullOrWhiteSpace(str))
            {
                throw new ArgumentNullException(name, "Input parameter is empty.");
            }
            if (obj == null)
                throw new ArgumentNullException(name, "Input parameter is null.");
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
                        await GarbageCollectAsync(token);
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
                catch (OperationCanceledException)
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
            return await _cacheLock.WriterLockAsync(token);
        }

        /// <summary>
        /// Locks CurrentFolder to read so it can't be changed in process.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<IDisposable> GlobalReadLock(CancellationToken token)
        {
            return await _cacheLock.ReaderLockAsync(token);
        }

        private async Task EnsureInitializedAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!_invalid)
                return;
            token.ThrowIfCancellationRequested();
            using (await GlobalWriteLock(token))
            {
                if (!_invalid)
                    return;
                token.ThrowIfCancellationRequested();
                var currentFolder = await GenerateIncrementTmpPath(_baseFolder, token);
                var tempFolder = Path.Combine(currentFolder, "tmp");
                var cacheFolder = Path.Combine(currentFolder, "cch");
                var trashFolder = Path.Combine(currentFolder, "bin");
                var transactionLogPath = Path.Combine(currentFolder, "journal.txt");
                var entries = new ConcurrentDictionary<TKey, CFileCacheEntry>();
                var actions = new ConcurrentBag<CancellableAction>();

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
            var actions = _actions;
            if (actions == null || !actions.Any())
                return;

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

            foreach (var r in toRetry)
            {
                actions.Add(r);
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
                if (await _fs.FileExistAsync(filePath, t))
                {
                    var newPath = await GetTmpFileAsync(_trashFolder, t);
                    await _fs.SetAttributesAsync(filePath, FileAttributes.Normal, t);
                    await _fs.MoveAsync(filePath, newPath, t);
                }
            }, token);
        }

        private async Task<string> MoveToCacheAsync(string filePath, CancellationToken token)
        {
            await Ensure(_cacheFolder, token);
            var newPath = await GetTmpFileAsync(_cacheFolder, token);
            await _fs.MoveAsync(filePath, newPath, token);
            await _fs.SetAttributesAsync(newPath, FileAttributes.ReadOnly | FileAttributes.NotContentIndexed, token);
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
            //at this point we are not responsible of disposing 'src' cause it's lifetime is wider than this method
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
            using (await _perKeyLock.LockAsync(key, token))
            {
                if (_entries.TryGetValue(key, out cacheEntry))
                    return cacheEntry.Pulse(policy);

                token.ThrowIfCancellationRequested();

                //we are responsible for creation and disposing of stream we created, so we wrap src in using directive.
                using (var src = await provider(key))
                {
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

        public async Task GarbageCollectAsync(CancellationToken token)
        {
            var now = DateTime.UtcNow;
            using (await GlobalWriteLock(token))
            {
                var nonCacheFolders = new List<string>();
                token.ThrowIfCancellationRequested();
                foreach (var dir in await _fs.GetDirectoriesAsync(_baseFolder, "fs*", SearchOption.TopDirectoryOnly, token))
                {
                    token.ThrowIfCancellationRequested();
                    if (!await _fs.EqualsAsync(dir, _currentFolder, token))
                        nonCacheFolders.Add(dir);
                }

                if (nonCacheFolders.Any())
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
            NotNull(provider, nameof(provider));
            NotNull(key, nameof(key));

            await EnsureInitializedAsync(token);
            using (await GlobalReadLock(token))
            {
                Func<TKey, Task<CFileSource>> op = async kk => new CFileSource(await provider(kk), false, _fs);
                var entry = await InternalGetOrAdd(key, op, token, policy);
                return await _fs.OpenReadAsync(entry.FilePath, token);
            }
        }

        public async Task<Stream> GetStreamOrAddFileAsync(TKey key, Func<TKey, Task<string>> provider, CancellationToken token, ICacheExpirationPolicy policy)
        {
            NotNull(provider, nameof(provider));
            NotNull(key, nameof(key));

            await EnsureInitializedAsync(token);
            using (await GlobalReadLock(token))
            {
                var entry = await InternalGetOrAdd(key, async kk => new CFileSource(await provider(kk), _fs), token, policy);
                return await _fs.OpenReadAsync(entry.FilePath, token);
            }
        }

        public async Task AddOrUpdateStreamAsync(TKey key, Stream stream, CancellationToken token, ICacheExpirationPolicy policy, bool leaveOpen = false)
        {
            NotNull(stream, nameof(stream));
            NotNull(key, nameof(key));

            using (var cfs = new CFileSource(stream, leaveOpen, _fs))
            {
                await EnsureInitializedAsync(token);
                using (await GlobalReadLock(token))
                {
                    await InternalAddOrUpdate(key, cfs, token, policy);
                }
            }
        }

        public async Task AddOrUpdateFileAsync(TKey key, string sourceFilePath, CancellationToken token, ICacheExpirationPolicy policy)
        {
            NotNull(sourceFilePath, nameof(sourceFilePath));
            NotNull(key, nameof(key));
            using (var cfs = new CFileSource(sourceFilePath, _fs))
            {
                await EnsureInitializedAsync(token);
                using (await GlobalReadLock(token))
                {
                    await InternalAddOrUpdate(key, cfs, token, policy);
                }
            }
        }

        public async Task GetFileOrAddStreamAsync(TKey key, Func<TKey, Task<Stream>> provider, CancellationToken token, string targetFilePath, ICacheExpirationPolicy policy)
        {
            NotNull(provider, nameof(provider));
            NotNull(key, nameof(key));
            NotNull(targetFilePath, nameof(targetFilePath));

            await EnsureInitializedAsync(token);
            using (await GlobalReadLock(token))
            {
                var entry = await InternalGetOrAdd(key, async kk => new CFileSource(await provider(kk), false, _fs), token, policy);
                using (var cfs = new CFileSource(entry.FilePath, _fs))
                {
                    await cfs.CopyToAsync(targetFilePath, token, true);
                }
            }
        }

        public async Task GetFileOrAddFileAsync(TKey key, Func<TKey, Task<string>> provider, CancellationToken token, string targetFilePath, ICacheExpirationPolicy policy)
        {
            NotNull(provider, nameof(provider));
            NotNull(key, nameof(key));
            NotNull(targetFilePath, nameof(targetFilePath));

            await EnsureInitializedAsync(token);
            using (await GlobalReadLock(token))
            {
                var entry = await InternalGetOrAdd(key, async kk => new CFileSource(await provider(kk), _fs), token, policy);
                using (var cfs = new CFileSource(entry.FilePath, _fs))
                {
                    await cfs.CopyToAsync(targetFilePath, token, true);
                }
            }
        }

        public async Task<bool> TryGetFileAsync(TKey key, CancellationToken token, string targetFilePath)
        {
            NotNull(key, nameof(key));
            NotNull(targetFilePath, nameof(targetFilePath));

            await EnsureInitializedAsync(token);
            using (await GlobalReadLock(token))
            {
                var entry = await InternalGetEntry(key, token);
                if (entry == null)
                    return false;

                using (var cfs = new CFileSource(entry.FilePath, _fs))
                {
                    await cfs.CopyToAsync(targetFilePath, token, true);
                }
                return true;
            }
        }

        public async Task<Stream> TryGetStreamAsync(TKey key, CancellationToken token)
        {
            NotNull(key, nameof(key));

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
            //_cacheLock.Dispose();
        }
    }

}
