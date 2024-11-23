using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Eocron.IO.Files;

namespace Eocron.IO.Caching
{
    public sealed class FileCache : IFileCache
    {
        public FileCache(FileSystem fs, HashAlgorithm hashAlgorithm, IFileCacheLockProvider lockProvider)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _hashAlgorithm = hashAlgorithm ?? throw new ArgumentNullException(nameof(hashAlgorithm));
            _lockProvider = lockProvider;
        }

        public async Task<bool> ContainsKeyAsync(string key, CancellationToken ct)
        {
            var entry = CreateActiveFileEntry(key, null);
            await using var _ = await ReadLock(entry.Hash, ct).ConfigureAwait(false);
            return await _fs.IsDirectoryExistAsync(entry.GetDirectoryPath(), ct).ConfigureAwait(false);
        }

        public async Task<IFileCacheLink> GetOrAddFileAsync(string key, FilePathProviderDelegate filePathProvider, bool retainSource = false,
            CancellationToken ct = default)
        {
            return await GetOrAddFileAsync(key, FileCacheShortNameHelper.GetRandom(), filePathProvider, retainSource, ct).ConfigureAwait(false);
        }

        public async Task<IFileCacheLink> GetOrAddFileAsync(string key, string fileName,
            FilePathProviderDelegate filePathProvider, bool retainSource = false,
            CancellationToken ct = default)
        {
            return await InternalGetOrAddAsync(key, fileName, ct, entry => retainSource ? CopyFromFile(entry, key, filePathProvider, ct) : MoveFromFile(entry, key, filePathProvider, ct))
                .ConfigureAwait(false);
        }

        public async Task<IFileCacheLink> GetOrAddStreamAsync(string key, StreamProviderDelegate streamProvider,
            CancellationToken ct = default)
        {
            return await GetOrAddStreamAsync(key, FileCacheShortNameHelper.GetRandom(), streamProvider, ct).ConfigureAwait(false);
        }

        public async Task<IFileCacheLink> GetOrAddStreamAsync(string key, string fileName,
            StreamProviderDelegate streamProvider,
            CancellationToken ct = default)
        {
            return await InternalGetOrAddAsync(key, fileName, ct, entry => UploadFromStream(entry, key, streamProvider, ct))
                .ConfigureAwait(false);
        }

        public async Task<bool> TryRemoveAsync(string key, CancellationToken ct)
        {
            var entry = CreateActiveFileEntry(key, null);
            await using var _ = await ReadLock(entry.Hash, ct).ConfigureAwait(false);
            if (!await _fs.IsDirectoryExistAsync(entry.GetDirectoryPath(), ct).ConfigureAwait(false))
                return false;

            await using var __ = await WriteLock(entry.Hash, ct).ConfigureAwait(false);
            return await _fs.TryDeleteDirectoryAsync(entry.GetDirectoryPath(), ct).ConfigureAwait(false);
        }

        private async Task<IFileCacheLink> InternalGetOrAddAsync(string key, string fileName, CancellationToken ct,
            Func<FileEntry, Task> writeAction)
        {
            var entry = CreateActiveFileEntry(key, fileName);
            var rl = await ReadLock(entry.Hash, ct).ConfigureAwait(false);
            try
            {
                if (await _fs.IsDirectoryExistAsync(entry.GetDirectoryPath(), ct).ConfigureAwait(false))
                    return await CreateFileCacheLink(entry, rl, ct).ConfigureAwait(false);
            }
            catch
            {
                await rl.DisposeAsync().ConfigureAwait(false);
                throw;
            }
            await rl.DisposeAsync().ConfigureAwait(false);

            
            var wl = await WriteLock(entry.Hash, ct).ConfigureAwait(false);
            try
            {
                if (await _fs.IsDirectoryExistAsync(entry.GetDirectoryPath(), ct).ConfigureAwait(false))
                    return await CreateFileCacheLink(entry, wl, ct).ConfigureAwait(false);
                
                await writeAction(entry).ConfigureAwait(false);
                return await CreateFileCacheLink(entry, wl, ct).ConfigureAwait(false);
            }
            catch
            {
                await wl.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        private FileEntry CreateActiveFileEntry(string virtualKey, string fileName)
        {
            var hash = GetHash(virtualKey);
            return new FileEntry(FileEntryState.Active, hash, fileName);
        }

        private async Task<IFileCacheLink> CreateFileCacheLink(FileEntry entry, IAsyncDisposable readLock, CancellationToken ct)
        {
            var hardLinkEntry = entry.CreateActiveHardLink();
            var hardLinkPhysicalFilePath = _fs.GetPhysicalFile(hardLinkEntry.GetFilePath()).FullName;
            await _fs.TryCreateDirectoryAsync(hardLinkEntry.GetDirectoryPath(), ct).ConfigureAwait(false);
            try
            {
                await _fs.CreateFileHardLinkAsync(entry.GetFilePath(), hardLinkEntry.GetFilePath(), ct)
                    .ConfigureAwait(false);
                return new FileCacheLink(hardLinkEntry, hardLinkPhysicalFilePath, this, readLock);
            }
            catch
            {
                await _fs.TryDeleteDirectoryAsync(hardLinkEntry.GetDirectoryPath(), ct).ConfigureAwait(false);
                throw;
            }
        }

        internal async Task DeleteHardLinkAsync(FileEntry hardLinkEntry, IAsyncDisposable readLock)
        {
            try
            {
                var dirPath = hardLinkEntry.GetDirectoryPath();
                await _fs.TryDeleteDirectoryAsync(dirPath).ConfigureAwait(false);
            }
            finally
            {
                await readLock.DisposeAsync().ConfigureAwait(false);
            }
        }

        private string GetHash(string virtualKey)
        {
            return FileCacheShortNameHelper.ToShortName(_hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(virtualKey)));
        }
        
        private async Task CopyFromFile(FileEntry entry, string key, FilePathProviderDelegate filePathProvider,
            CancellationToken ct)
        {
            var activePhysicalFilePath = _fs.GetPhysicalFile(entry.GetFilePath()).FullName;
            await _fs.TryCreateDirectoryAsync(entry.GetDirectoryPath(), ct).ConfigureAwait(false);
            try
            {
                var physicalFilePath = await filePathProvider(key, ct).ConfigureAwait(false);
                File.Copy(physicalFilePath, activePhysicalFilePath);
            }
            catch
            {
                await _fs.TryDeleteDirectoryAsync(entry.GetDirectoryPath(), ct).ConfigureAwait(false);
                throw;
            }
        }

        private async Task MoveFromFile(FileEntry entry, string key, FilePathProviderDelegate filePathProvider,
            CancellationToken ct)
        {
            var activePhysicalFilePath = _fs.GetPhysicalFile(entry.GetFilePath()).FullName;
            await _fs.TryCreateDirectoryAsync(entry.GetDirectoryPath(), ct).ConfigureAwait(false);
            try
            {
                var physicalFilePath = await filePathProvider(key, ct).ConfigureAwait(false);
                File.Move(physicalFilePath, activePhysicalFilePath);
            }
            catch
            {
                await _fs.TryDeleteDirectoryAsync(entry.GetDirectoryPath(), ct).ConfigureAwait(false);
                throw;
            }
        }

        private async Task UploadFromStream(FileEntry entry, string key, StreamProviderDelegate streamProvider,
            CancellationToken ct)
        {
            var tmpEntry = entry.CreateTemporal();
            await _fs.TryCreateDirectoryAsync(tmpEntry.GetDirectoryPath(), ct).ConfigureAwait(false);
            try
            {
                await using (var inputStream = await streamProvider(key, ct).ConfigureAwait(false))
                {
                    await using var outputStream = await _fs
                        .OpenFileAsync(tmpEntry.GetFilePath(), FileMode.CreateNew, ct).ConfigureAwait(false);
                    await inputStream.CopyToAsync(outputStream, ct).ConfigureAwait(false);
                }

                await _fs.MoveFileAsync(tmpEntry.GetFilePath(), entry.GetFilePath(), ct).ConfigureAwait(false);
            }
            finally
            {
                await _fs.TryDeleteDirectoryAsync(tmpEntry.GetDirectoryPath(), ct).ConfigureAwait(false);
            }
        }
        
        private async Task<IAsyncDisposable> ReadLock(string hash, CancellationToken ct)
        {
            return await _lockProvider.LockReadAsync(hash, ct).ConfigureAwait(false);
        }

        private async Task<IAsyncDisposable> WriteLock(string hash, CancellationToken ct)
        {
            return await _lockProvider.LockWriteAsync(hash, ct).ConfigureAwait(false);
        }
        
        private async Task<IAsyncDisposable> UpgradeToWriteLock(string hash, CancellationToken ct)
        {
            return await _lockProvider.LockUpgradeWriteAsync(hash, ct).ConfigureAwait(false);
        }

        private readonly FileSystem _fs;
        private readonly HashAlgorithm _hashAlgorithm;
        private readonly IFileCacheLockProvider _lockProvider;
    }
}