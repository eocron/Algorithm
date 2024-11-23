using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.Disposables;

namespace Eocron.IO.Caching
{
    public sealed class InMemoryFileCacheLockProvider : IFileCacheLockProvider
    {
        public async Task<IAsyncDisposable> LockReadAsync(string key, CancellationToken ct)
        {
            var rw = GetOrAdd(key);
            var r = await rw.ReaderLockAsync(ct).ConfigureAwait(false);
            return r.ToAsyncDisposable();
        }

        private AsyncReaderWriterLock GetOrAdd(string key)
        {
            return _cache.GetOrAdd(key, _ => new Lazy<AsyncReaderWriterLock>(() => new AsyncReaderWriterLock())).Value;
        }

        public async Task<IAsyncDisposable> LockWriteAsync(string key, CancellationToken ct)
        {
            var rw = GetOrAdd(key);
            var r = await rw.WriterLockAsync(ct).ConfigureAwait(false);
            return r.ToAsyncDisposable();
        }

        public async Task<IAsyncDisposable> LockUpgradeWriteAsync(string key, CancellationToken ct)
        {
            var rw = GetOrAdd(key);
            var r = await rw.WriterLockAsync(ct).ConfigureAwait(false);
            return r.ToAsyncDisposable();
        }
        
        private readonly ConcurrentDictionary<string, Lazy<AsyncReaderWriterLock>> _cache = new();
        
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