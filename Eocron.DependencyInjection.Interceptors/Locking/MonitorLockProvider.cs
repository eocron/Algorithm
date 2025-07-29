using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.DependencyInjection.Interceptors.Locking
{
    public sealed class MonitorLockProvider : ILockProvider
    {
        private readonly object _sync = new();
        private readonly TimeSpan _waitInterval = TimeSpan.FromMilliseconds(100);

        public Task<IAsyncDisposable> AcquireAsync(CancellationToken ct)
        {
            throw new NotSupportedException("This lock is only supported on IDisposable.");
        }

        public IDisposable Acquire(CancellationToken ct)
        {
            while(!Monitor.TryEnter(_sync, _waitInterval))
            {
                ct.ThrowIfCancellationRequested();
            }
            return new Releaser(_sync);
        }

        private sealed class Releaser(object sync) : IDisposable
        {
            public void Dispose()
            {
                Monitor.Exit(sync);
            }
        }
    }
}