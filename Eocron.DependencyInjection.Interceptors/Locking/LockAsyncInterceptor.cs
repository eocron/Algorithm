using System;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Eocron.DependencyInjection.Interceptors.Locking
{
    public sealed class LockAsyncInterceptor : IAsyncInterceptor, IDisposable
    {
        private readonly ILockProvider _lockProvider;
        private readonly bool _disposeProvider;

        public LockAsyncInterceptor(
            ILockProvider lockProvider,
            bool disposeProvider)
        {
            _lockProvider = lockProvider;
            _disposeProvider = disposeProvider;
        }

        public void InterceptSynchronous(IInvocation invocation)
        {
            ExecuteSync(invocation);
        }

        public void InterceptAsynchronous(IInvocation invocation)
        {
            invocation.ReturnValue = ExecuteAsync(invocation);
        }

        public void InterceptAsynchronous<TResult>(IInvocation invocation)
        {
            invocation.ReturnValue = ExecuteAsync<TResult>(invocation);
        }

        private void ExecuteSync(IInvocation invocation)
        {
            var ct = InterceptionHelper.GetCancellationTokenOrDefault(invocation);
            using var sync = _lockProvider.Acquire(ct);
            invocation.Proceed();
        }

        private async Task ExecuteAsync(IInvocation invocation)
        {
            var ct = InterceptionHelper.GetCancellationTokenOrDefault(invocation);
            await using var sync = await _lockProvider.AcquireAsync(ct).ConfigureAwait(false);
            invocation.Proceed();
            var task = (Task)invocation.ReturnValue;
            await task.ConfigureAwait(false);
        }

        private async Task<T> ExecuteAsync<T>(IInvocation invocation)
        {
            var ct = InterceptionHelper.GetCancellationTokenOrDefault(invocation);
            await using var sync = await _lockProvider.AcquireAsync(ct).ConfigureAwait(false);
            invocation.Proceed();
            var task = (Task<T>)invocation.ReturnValue;
            return await task.ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_disposeProvider)
            {
                (_lockProvider as IDisposable)?.Dispose();
            }
        }
    }
}