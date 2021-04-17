using Eocron.Algorithms.Disposing;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.FileCache
{
    /// <summary>
    /// Read-preffered ReaderWriterLockSlim implementation.
    /// </summary>
    public sealed class AsyncReaderWriterLock : IDisposable
    {
        private readonly SemaphoreSlim _r = new SemaphoreSlim(1);
        private readonly SemaphoreSlim _g = new SemaphoreSlim(1);
        private int _b = 0;

        public async Task<IDisposable> ReaderLockAsync(CancellationToken token)
        {
            await _r.WaitAsync(token);
            try
            {
                _b++;
                if (_b == 1)
                {
                    await _g.WaitAsync(token);//cancellation exception here
                }
                return new Disposable(() => ReleaseRead());
            }
            catch (OperationCanceledException)
            {
                _b--;
                throw;
            }
            finally
            {
                _r.Release();
            }
        }

        private void ReleaseRead()
        {
            _r.Wait();
            try
            {
                _b--;
                if (_b == 0)
                    _g.Release();
            }
            finally
            {
                _r.Release();
            }
        }

        public async Task<IDisposable> WriterLockAsync(CancellationToken token)
        {
            await _g.WaitAsync(token);
            return new Disposable(() => ReleaseWrite());
        }

        private void ReleaseWrite()
        {
            _g.Release();
        }

        public void Dispose()
        {
            _r.Dispose();
            _g.Dispose();
            _b = 0;
        }
    }
}
