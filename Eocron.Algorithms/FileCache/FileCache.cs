﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Eocron.Algorithms.Disposing;

namespace Eocron.Algorithms.FileCache
{
    public sealed class FileCache<TKey> : IFileCache<TKey>, IDisposable
    {
        /// <summary>
        ///     File cache default ctr.
        /// </summary>
        /// <param name="baseFolder">Folder in which this instance of file cache will reside.</param>
        /// <param name="fileSystem">File system interface to use in all file operation. Defaul is current file system.</param>
        /// <param name="disableGc">Check if you will manage garbage collection yourself.</param>
        public FileCache(string baseFolder, IFileSystem fileSystem = null, bool disableGc = false)
        {
            _perKeyLock = new PerKeySemaphoreSlim();
            _cacheLock = new ReaderWriterLockSlim();
            _fs = fileSystem ?? FileSystem.Instance;
            BaseFolder = baseFolder ?? throw new ArgumentNullException(nameof(baseFolder));
            _invalid = true;

            if (!disableGc)
            {
                _cts = new CancellationTokenSource();
                _gc = new Thread(() => GcTask(_cts.Token));
                _gc.Start();
            }
        }

        public void AddOrUpdateFile(TKey key, string sourceFilePath, CancellationToken token,
            ICacheExpirationPolicy policy)
        {
            NotNull(sourceFilePath, nameof(sourceFilePath));
            NotNull(key, nameof(key));
            using var cfs = new CFileSource(sourceFilePath, _fs);
            EnsureInitialized(token);
            using (GlobalReadLock(token))
            {
                InternalAddOrUpdate(key, cfs, token, policy);
            }
        }

        public void AddOrUpdateStream(TKey key, Stream stream, CancellationToken token, ICacheExpirationPolicy policy,
            bool leaveOpen = false)
        {
            NotNull(stream, nameof(stream));
            NotNull(key, nameof(key));

            using var cfs = new CFileSource(stream, leaveOpen, _fs);
            EnsureInitialized(token);
            using (GlobalReadLock(token))
            {
                InternalAddOrUpdate(key, cfs, token, policy);
            }
        }

        public void Dispose()
        {
            _cts?.Dispose();
            _gc?.Join();
            _perKeyLock.Dispose();
            //_cacheLock.Dispose();
        }

        public void GarbageCollect(CancellationToken token)
        {
            var now = DateTime.UtcNow;
            using (GlobalWriteLock(token))
            {
                var nonCacheFolders = new List<string>();
                token.ThrowIfCancellationRequested();
                foreach (var dir in _fs.GetDirectories(BaseFolder, "fs*", SearchOption.TopDirectoryOnly, token))
                {
                    token.ThrowIfCancellationRequested();
                    if (!_fs.Equals(dir, _currentFolder, token))
                        nonCacheFolders.Add(dir);
                }

                if (nonCacheFolders.Any())
                    foreach (var dir in nonCacheFolders)
                    {
                        token.ThrowIfCancellationRequested();
                        Delete(dir, token);
                    }

                ShouldExecuteIteration(token);

                token.ThrowIfCancellationRequested();
                if (_entries != null && _entries.Any())
                {
                    var expired = _entries.Where(x => x.Value.IsExpired(now)).Select(x => x.Key);
                    foreach (var key in expired)
                    {
                        token.ThrowIfCancellationRequested();
                        CFileCacheEntry entry;
                        if (_entries.TryRemove(key, out entry)) MoveToTrash(entry.FilePath, token);
                    }
                }

                token.ThrowIfCancellationRequested();
                if (_trashFolder != null && _fs.DirectoryExist(_trashFolder, token))
                {
                    token.ThrowIfCancellationRequested();
                    foreach (var file in _fs.GetFiles(_trashFolder, "*", SearchOption.TopDirectoryOnly, token))
                    {
                        token.ThrowIfCancellationRequested();
                        Delete(file, token);
                    }
                }
            }
        }

        public void GetFileOrAddFile(TKey key, Func<TKey, string> provider, CancellationToken token,
            string targetFilePath, ICacheExpirationPolicy policy)
        {
            NotNull(provider, nameof(provider));
            NotNull(key, nameof(key));
            NotNull(targetFilePath, nameof(targetFilePath));

            EnsureInitialized(token);
            using (GlobalReadLock(token))
            {
                var entry = InternalGetOrAdd(key, kk => new CFileSource(provider(kk), _fs), token, policy);
                using (var cfs = new CFileSource(entry.FilePath, _fs))
                {
                    cfs.CopyTo(targetFilePath, token, true);
                }
            }
        }

        public void GetFileOrAddStream(TKey key, Func<TKey, Stream> provider, CancellationToken token,
            string targetFilePath, ICacheExpirationPolicy policy)
        {
            NotNull(provider, nameof(provider));
            NotNull(key, nameof(key));
            NotNull(targetFilePath, nameof(targetFilePath));

            EnsureInitialized(token);
            using (GlobalReadLock(token))
            {
                var entry = InternalGetOrAdd(key, kk => new CFileSource(provider(kk), false, _fs), token, policy);
                using (var cfs = new CFileSource(entry.FilePath, _fs))
                {
                    cfs.CopyTo(targetFilePath, token, true);
                }
            }
        }

        public Stream GetStreamOrAddFile(TKey key, Func<TKey, string> provider, CancellationToken token,
            ICacheExpirationPolicy policy)
        {
            NotNull(provider, nameof(provider));
            NotNull(key, nameof(key));

            EnsureInitialized(token);
            using (GlobalReadLock(token))
            {
                var entry = InternalGetOrAdd(key, kk => new CFileSource(provider(kk), _fs), token, policy);
                return _fs.OpenRead(entry.FilePath, token);
            }
        }

        public Stream GetStreamOrAddStream(TKey key, Func<TKey, Stream> provider, CancellationToken token,
            ICacheExpirationPolicy policy)
        {
            NotNull(provider, nameof(provider));
            NotNull(key, nameof(key));

            EnsureInitialized(token);
            using (GlobalReadLock(token))
            {
                Func<TKey, CFileSource> op = kk => new CFileSource(provider(kk), false, _fs);
                var entry = InternalGetOrAdd(key, op, token, policy);
                return _fs.OpenRead(entry.FilePath, token);
            }
        }

        public void Invalidate(CancellationToken token)
        {
            _invalid = true;
        }

        public void Invalidate(TKey key, CancellationToken token)
        {
            if (_invalid) //no meaning to invalidate if cache is entirely invalidated.
                return;

            using (GlobalReadLock(token))
            {
                if (_invalid)
                    return;

                CFileCacheEntry entry;
                if (!_entries.TryRemove(key, out entry))
                    return;

                MoveToTrash(entry.FilePath, token);
            }
        }

        public bool TryGetFile(TKey key, CancellationToken token, string targetFilePath)
        {
            NotNull(key, nameof(key));
            NotNull(targetFilePath, nameof(targetFilePath));

            EnsureInitialized(token);
            using (GlobalReadLock(token))
            {
                var entry = InternalGetEntry(key, token);
                if (entry == null)
                    return false;

                using (var cfs = new CFileSource(entry.FilePath, _fs))
                {
                    cfs.CopyTo(targetFilePath, token, true);
                }

                return true;
            }
        }

        public Stream TryGetStream(TKey key, CancellationToken token)
        {
            NotNull(key, nameof(key));

            EnsureInitialized(token);
            using (GlobalReadLock(token))
            {
                var entry = InternalGetEntry(key, token);
                if (entry == null)
                    return null;

                return _fs.OpenRead(entry.FilePath, token);
            }
        }

        private void GcTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
                try
                {
                    var failed = false;
                    try
                    {
                        GarbageCollect(token);
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
                    Thread.Sleep(failed ? _gcFailRetryIntervalMs : _gcIntervalMs);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
        }

        private void NotNull(object obj, string name)
        {
            var str = obj as string;
            if (str != null && string.IsNullOrWhiteSpace(str))
                throw new ArgumentNullException(name, "Input parameter is empty.");
            if (obj == null)
                throw new ArgumentNullException(name, "Input parameter is null.");
        }

        public string BaseFolder { get; }

        public string CurrentFolder
        {
            get
            {
                EnsureInitialized(CancellationToken.None);
                return _currentFolder;
            }
        }

        private static long _uniqueIdCounter;
        private readonly CancellationTokenSource _cts;
        private readonly PerKeySemaphoreSlim _perKeyLock;
        private readonly IFileSystem _fs;
        private readonly int _gcFailRetryIntervalMs = 10 * 1000;

        private readonly int _gcIntervalMs = 5 * 1000;
        private readonly ReaderWriterLockSlim _cacheLock;
        private readonly Thread _gc;

        private volatile bool _invalid;
        private ConcurrentBag<CancellableAction> _actions;
        private ConcurrentDictionary<TKey, CFileCacheEntry> _entries;
        private string _cacheFolder;
        private string _currentFolder;
        private string _tempFolder;
        private string _trashFolder;

        #region Private helper classes

        private delegate void CancellableAction(CancellationToken token);

        private sealed class PerKeySemaphoreSlim : IDisposable
        {
            public void Dispose()
            {
                _lock.Dispose();
                foreach (var v in _dict.Values) v.Value.Dispose();
            }

            public IDisposable Lock(object key, CancellationToken token)
            {
                var item = GetOrCreate(key, token);
                item.Value.Wait(CancellationToken.None);
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

            private RefCounted<SemaphoreSlim> GetOrCreate(object key, CancellationToken token)
            {
                RefCounted<SemaphoreSlim> item;
                _lock.Wait(token);
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

            private readonly Dictionary<object, RefCounted<SemaphoreSlim>> _dict =
                new Dictionary<object, RefCounted<SemaphoreSlim>>();

            private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);

            private sealed class RefCounted<T>
            {
                public RefCounted(T value)
                {
                    RefCount = 1;
                    Value = value;
                }

                public int RefCount { get; set; }
                public T Value { get; }
            }
        }

        private sealed class CFileCacheEntry : AnyExpirationPolicy
        {
            public string FilePath { get; set; }
        }

        private sealed class CFileSource : IDisposable
        {
            public CFileSource(string filePath, IFileSystem fs)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    throw new ArgumentNullException(nameof(filePath), "Source path is null or empty.");
                _fs = fs;
                FilePath = filePath;
            }

            public CFileSource(Stream stream, bool leaveOpen, IFileSystem fs)
            {
                _leaveOpen = leaveOpen;
                _fs = fs;
                Stream = stream ?? throw new ArgumentNullException(nameof(stream), "Stream is null.");
            }

            public void CopyTo(string path, CancellationToken token, bool createHardLink)
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentNullException(nameof(path), "Target path is null or empty.");

                if (Stream != null)
                {
                    Task.Run(async () =>
                        {
                            using var fstream = _fs.OpenCreate(path, token);
                            await Stream.CopyToAsync(fstream, _uploadBufferSize, token);
                        }, token)
                        .GetAwaiter().GetResult();
                }
                else
                {
                    if (createHardLink)
                        _fs.CreateHardLink(FilePath, path, token);
                    else
                        _fs.CopyFile(FilePath, path, token);
                }

                _fs.SetAttributes(path, FileAttributes.Normal, token);
            }

            public void Dispose()
            {
                if (!_leaveOpen && Stream != null)
                {
                    try
                    {
                        Stream.Close();
                    }
                    catch
                    {
                        // ignored
                    }

                    try
                    {
                        Stream.Dispose();
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            private Stream Stream { get; }
            private string FilePath { get; }
            private readonly bool _leaveOpen;
            private readonly IFileSystem _fs;
            private readonly int _uploadBufferSize = 81920;
        }

        #endregion


        #region Private methods

        private static string GetUniqueFileName()
        {
            //it is very unlikelly to iterate over entire uniqueId and come back to collision.
            var id = unchecked((ulong)Interlocked.Increment(ref _uniqueIdCounter));
            return id.ToString("x8");
        }

        /// <summary>
        ///     Locks CurrentFolder to write so it can be modified in process.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private IDisposable GlobalWriteLock(CancellationToken token)
        {
            _cacheLock.EnterWriteLock();
            return new Disposable(() => _cacheLock.ExitWriteLock());
        }

        /// <summary>
        ///     Locks CurrentFolder to read so it can't be changed in process.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private IDisposable GlobalReadLock(CancellationToken token)
        {
            _cacheLock.EnterReadLock();
            return new Disposable(() => _cacheLock.ExitReadLock());
        }

        private void EnsureInitialized(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!_invalid)
                return;
            token.ThrowIfCancellationRequested();
            using (GlobalWriteLock(token))
            {
                if (!_invalid)
                    return;
                token.ThrowIfCancellationRequested();
                var currentFolder = GenerateIncrementTmpPath(BaseFolder, token);
                var tempFolder = Path.Combine(currentFolder, "tmp");
                var cacheFolder = Path.Combine(currentFolder, "cch");
                var trashFolder = Path.Combine(currentFolder, "bin");
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
        ///     Enforces execution of particular method. It will be retried on each garbage collect.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private void ShouldExecute(CancellableAction action, CancellationToken token)
        {
            try
            {
                action(token);
                return;
            }
            catch
            {
                // ignored
            }

            _actions.Add(action);
        }

        private void ShouldExecuteIteration(CancellationToken token)
        {
            var actions = _actions;
            if (actions == null || !actions.Any())
                return;

            CancellableAction a;

            var toRetry = new List<CancellableAction>();
            while (_actions.TryTake(out a))
                try
                {
                    a(token);
                }
                catch
                {
                    toRetry.Add(a);
                }

            foreach (var r in toRetry) actions.Add(r);
        }


        private void Ensure(string dir, CancellationToken token)
        {
            _fs.CreateDirectory(dir, token);
        }

        private string GenerateIncrementTmpPath(string folder, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Ensure(folder, token);
            do
            {
                token.ThrowIfCancellationRequested();
                var path = Path.Combine(folder, "fs" + GetUniqueFileName());
                if (!_fs.FileExist(path, token) && !_fs.DirectoryExist(path, token))
                    return path;
            } while (true);
        }

        private string GetTmpFile(string folder, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Ensure(folder, token);
            while (true)
            {
                token.ThrowIfCancellationRequested();

                var path = Path.Combine(folder, GetUniqueFileName());
                if (_fs.FileExist(path, token))
                    continue;

                return path;
            }
        }

        private void MoveToTrash(string filePath, CancellationToken token)
        {
            ShouldExecute(t =>
            {
                Ensure(_trashFolder, t);
                //new ZlpFileInfo(filePath).Attributes = FileAttributes.Normal;
                if (_fs.FileExist(filePath, t))
                {
                    var newPath = GetTmpFile(_trashFolder, t);
                    _fs.SetAttributes(filePath, FileAttributes.Normal, t);
                    _fs.Move(filePath, newPath, t);
                }
            }, token);
        }

        private string MoveToCache(string filePath, CancellationToken token)
        {
            Ensure(_cacheFolder, token);
            var newPath = GetTmpFile(_cacheFolder, token);
            _fs.Move(filePath, newPath, token);
            _fs.SetAttributes(newPath, FileAttributes.ReadOnly | FileAttributes.NotContentIndexed, token);
            //new ZlpFileInfo(newPath).Attributes |= FileAttributes.Readonly;
            return newPath;
        }

        private string GetTmpUploadFilePath(CancellationToken token)
        {
            return GetTmpFile(_tempFolder, token);
        }

        private string UploadToCache(CFileSource src, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var path = GetTmpUploadFilePath(token);
            try
            {
                src.CopyTo(path, token, false);
                token.ThrowIfCancellationRequested();
                return MoveToCache(path, token);
            }
            catch
            {
                if (_fs.FileExist(path, token))
                    MoveToTrash(path, token);
                throw;
            }
        }

        private void InternalAddOrUpdate(TKey key, CFileSource src, CancellationToken token,
            ICacheExpirationPolicy policy)
        {
            //at this point we are not responsible of disposing 'src' cause it's lifetime is wider than this method
            var path = UploadToCache(src, token);
            _entries[key] = new CFileCacheEntry
            {
                FilePath = path
            }.Pulse(policy);
        }

        private CFileCacheEntry InternalGetOrAdd(TKey key, Func<TKey, CFileSource> provider, CancellationToken token,
            ICacheExpirationPolicy policy)
        {
            CFileCacheEntry cacheEntry;

            if (_entries.TryGetValue(key, out cacheEntry))
                return cacheEntry.Pulse(policy);

            //per key mutex required to avoid multiple file upload to cache by same key.
            //files are very large objects, so unnecessary actions with them should be avoided if possible.
            using (_perKeyLock.Lock(key, token))
            {
                if (_entries.TryGetValue(key, out cacheEntry))
                    return cacheEntry.Pulse(policy);

                token.ThrowIfCancellationRequested();

                //we are responsible for creation and disposing of stream we created, so we wrap src in using directive.
                using (var src = provider(key))
                {
                    token.ThrowIfCancellationRequested();
                    var path = UploadToCache(src, token);
                    cacheEntry = new CFileCacheEntry
                    {
                        FilePath = path
                    };
                    //######
                    _entries[key] = cacheEntry;

                    return cacheEntry.Pulse(policy);
                }
            }
        }

        private CFileCacheEntry InternalGetEntry(TKey key, CancellationToken token)
        {
            return _entries.TryGetValue(key, out var cacheEntry) ? cacheEntry.Pulse(null) : null;
        }


        private void SafeDeleteFile(string path, CancellationToken token)
        {
            try
            {
                _fs.DeleteFile(path, token);
            }
            catch
            {
                // ignored
            }
        }

        private void Delete(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (_fs.FileExist(path, token))
            {
                token.ThrowIfCancellationRequested();
                SafeDeleteFile(path, token);
            }

            token.ThrowIfCancellationRequested();
            if (_fs.DirectoryExist(path, token))
            {
                token.ThrowIfCancellationRequested();
                foreach (var file in _fs.GetFiles(path, "*", SearchOption.TopDirectoryOnly, token))
                {
                    token.ThrowIfCancellationRequested();
                    SafeDeleteFile(file, token);
                }

                token.ThrowIfCancellationRequested();
                foreach (var dir in _fs.GetDirectories(path, "*", SearchOption.TopDirectoryOnly, token))
                {
                    token.ThrowIfCancellationRequested();
                    Delete(dir, token);
                }

                token.ThrowIfCancellationRequested();
                try
                {
                    _fs.DeleteDirectoryNonRecursive(path, token);
                }
                catch
                {
                    // ignored
                }
            }
        }

        #endregion
    }
}