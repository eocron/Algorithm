using System;
using System.IO;
using System.Threading.Tasks;

namespace Eocron.Algorithms.Caching
{
    internal sealed class FileCacheLink : IFileCacheLink
    {
        internal FileCacheLink(FileEntry entry, string fileHardLinkPath, FileCache cache, IAsyncDisposable readLock)
        {
            _entry = entry;
            _fileHardLinkPath = fileHardLinkPath;
            _cache = cache;
            _readLock = readLock;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            await _cache.DeleteHardLinkAsync(_entry, _readLock).ConfigureAwait(false);
            _disposed = true;
        }

        public Stream OpenRead()
        {
            CheckDisposed();
            return File.OpenRead(_fileHardLinkPath);
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnsafeFilePath), "Hard link is disposed and can't be used.");
        }

        public string UnsafeFilePath
        {
            get
            {
                CheckDisposed();
                return _fileHardLinkPath;
            }
        }

        private readonly FileEntry _entry;
        private readonly string _fileHardLinkPath;
        private readonly FileCache _cache;
        private readonly IAsyncDisposable _readLock;

        private bool _disposed;
    }
}