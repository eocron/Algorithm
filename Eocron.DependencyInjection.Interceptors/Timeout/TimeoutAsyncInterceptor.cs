using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Eocron.DependencyInjection.Interceptors.Timeout
{
    public sealed class TimeoutAsyncInterceptor : AsyncInterceptorBase
    {
        private readonly TimeSpan _timeout;

        public TimeoutAsyncInterceptor(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero || timeout == System.Threading.Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException($"Invalid timeout provided: {timeout}");
            }

            _timeout = timeout;

        }

        protected override async Task InterceptAsync(IInvocation invocation, IInvocationProceedInfo proceedInfo,
            Func<IInvocation, IInvocationProceedInfo, Task> proceed)
        {
            await InterceptAsync(invocation, proceedInfo, async (x,y) =>
            {
                await proceed(x, y).ConfigureAwait(false);
                return (object)null;
            }).ConfigureAwait(false);
        }

        protected override async Task<TResult> InterceptAsync<TResult>(IInvocation invocation,
            IInvocationProceedInfo proceedInfo, Func<IInvocation, IInvocationProceedInfo, Task<TResult>> proceed)
        {
            var rootCt = InterceptionHelper.GetCancellationTokenOrDefault(invocation);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(rootCt);
            InterceptionHelper.TryReplaceCancellationToken(invocation, cts.Token);

            var task = Task.Run(() => proceed(invocation, proceedInfo));
            var completedTask = await Task.WhenAny(task, Task.Delay(_timeout, cts.Token));
            if (completedTask == task || rootCt.IsCancellationRequested)
            {
                await cts.CancelAsync().ConfigureAwait(false);
                return await task.ConfigureAwait(false); // Very important in order to propagate exceptions
            }

            throw new TimeoutException($"Method {invocation} timed out after {_timeout}");
        }
    }
}