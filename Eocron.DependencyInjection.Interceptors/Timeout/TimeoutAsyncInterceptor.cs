using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Eocron.DependencyInjection.Interceptors
{
    public sealed class TimeoutAsyncInterceptor : AsyncInterceptorBase
    {
        private readonly TimeSpan _timeout;

        public TimeoutAsyncInterceptor(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero || timeout == Timeout.InfiniteTimeSpan)
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

            var tcs = new TaskCompletionSource<TResult>();

            async Task TimeoutJob()
            {
                await InterceptionHelper.SafeDelay(_timeout).ConfigureAwait(false);
                tcs.TrySetException(CreateTimeoutException(invocation));
            }

            async Task ProceedJob()
            {
                await Task.Yield();
                cts.CancelAfter(_timeout);
                TResult result;
                try
                {
                    result = await proceed(invocation, proceedInfo);
                    if (IsTimedOut())
                    {
                        tcs.TrySetException(CreateTimeoutException(invocation));
                    }
                    else
                    {
                        tcs.TrySetResult(result);
                    }
                }
                catch (Exception e) when (IsTimedOut())
                {
                    tcs.TrySetException(CreateTimeoutException(invocation, e));
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            }

            bool IsTimedOut()
            {
                return cts.IsCancellationRequested && !rootCt.IsCancellationRequested;
            }

            
#pragma warning disable CS4014
            TimeoutJob();
            ProceedJob();
#pragma warning restore CS4014
            return await tcs.Task.ConfigureAwait(false);
        }

        private Exception CreateTimeoutException(IInvocation invocation, Exception e = null)
        {
            return new TimeoutException($"Method {invocation} timed out after {_timeout}", e);
        }
    }
}