using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.DependencyInjection.Interceptors.Caching
{
    public sealed class AsyncAtomicLazy<T>
    {
        private readonly Func<Task<T>> _factory;
    
        private Task<T> _task;

        private bool _initialized;

        private object _lock;

        public AsyncAtomicLazy(Func<Task<T>> factory)
        {
            _factory = factory;
        }

        public async Task<T> Value()
        {
            try
            {
                return await LazyInitializer.EnsureInitialized(ref _task, ref _initialized, ref _lock, _factory);
            }
            catch
            {
                Volatile.Write(ref _initialized, false);
                throw;
            }
        }
    }
}