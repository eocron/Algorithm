using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.ProxyHost
{
    public sealed class ThreadSafeProxy : IProxy
    {
        private readonly IProxy _inner;
        private readonly SemaphoreSlim _sync;
        private volatile bool _running;

        public ThreadSafeProxy(IProxy inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _sync = new SemaphoreSlim(1);
        }

        public async Task StartAsync(CancellationToken ct)
        {
            if (_running)
                throw CreateAlreadyStartedException();
            await _sync.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_running)
                    throw CreateAlreadyStartedException();

                await _inner.StartAsync(ct).ConfigureAwait(false);
                _running = true;
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task StopAsync(CancellationToken ct)
        {
            if(!_running)
                return;
            await _sync.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if(!_running)
                    return;

                await _inner.StopAsync(ct).ConfigureAwait(false);
                _running = false;
            }
            finally
            {
                _sync.Release();
            }
        }
        
        private static Exception CreateAlreadyStartedException()
        {
            return new InvalidOperationException("Proxy already started.");
        }
    }
}