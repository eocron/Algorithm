using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.IO.Caching
{
    public class InMemoryFileCacheLockProvider : IFileCacheLockProvider
    {
        public ConcurrentDictionary<string, Lazy<ReaderWriterLockSlim>> _cache =
            new ConcurrentDictionary<string, Lazy<ReaderWriterLockSlim>>();
        public async Task<IAsyncDisposable> LockReadAsync(string key, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        private ReaderWriterLockSlim GetOrAdd(string key)
        {
            return _cache.GetOrAdd(key, _ => new Lazy<ReaderWriterLockSlim>(() => new ReaderWriterLockSlim())).Value;
        }

        public async Task<IAsyncDisposable> LockWriteAsync(string key, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public async Task<IAsyncDisposable> LockUpgradeWriteAsync(string key, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
        
        private sealed class Lock : IAsyncDisposable
        {
            private readonly Func<ValueTask> _dispose;

            public Lock(Func<ValueTask> dispose)
            {
                _dispose = dispose;
            }

            public async ValueTask DisposeAsync()
            {
                await _dispose().ConfigureAwait(false);
            }
        }
    }
}