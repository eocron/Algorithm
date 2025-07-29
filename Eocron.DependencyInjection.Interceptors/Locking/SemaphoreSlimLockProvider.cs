using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.DependencyInjection.Interceptors.Locking
{
    public sealed class SemaphoreSlimLockProvider : ILockProvider, IDisposable
    {
        private readonly SemaphoreSlim _sync;

        public SemaphoreSlimLockProvider(int count)
        {
            _sync = new SemaphoreSlim(count);
        }

        public async Task<IAsyncDisposable> AcquireAsync(CancellationToken ct)
        {
            await _sync.WaitAsync(ct).ConfigureAwait(false);
            return new Releaser(_sync);
        }

        public IDisposable Acquire(CancellationToken ct)
        { 
            _sync.Wait(ct);
            return new Releaser(_sync);
        }

        private sealed class Releaser(SemaphoreSlim semaphoreSlim) : IAsyncDisposable, IDisposable
        {
            public ValueTask DisposeAsync()
            {
                semaphoreSlim.Release();
                return ValueTask.CompletedTask;
            }

            public void Dispose()
            {
                semaphoreSlim.Release();
            }
        }

        public void Dispose()
        {
            _sync?.Dispose();
        }
    }
}