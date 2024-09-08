using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Eocron.Aspects
{
    public sealed class TimeoutAsyncInterceptor : AsyncInterceptorBase
    {
        private readonly TimeSpan _timeout;

        public TimeoutAsyncInterceptor(TimeSpan timeout)
        {
            _timeout = timeout;
        }

        protected override async Task InterceptAsync(IInvocation invocation, IInvocationProceedInfo proceedInfo, Func<IInvocation, IInvocationProceedInfo, Task> proceed)
        {
            var rootCt = InterceptionHelper.GetCancellationTokenOrDefault(invocation);

            
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(rootCt);
            InterceptionHelper.TryReplaceCancellationToken(invocation, cts.Token);
            async Task To()
            {
                await Task.Delay(_timeout, rootCt).ConfigureAwait(false);
                cts.Cancel();
                throw new TimeoutException($"Method {invocation} timed out after {_timeout}");
            }

            try
            {
                await await Task.WhenAny(
                        Task.Run(async () => await proceed(invocation, proceedInfo), cts.Token),
                        To())
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (rootCt.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Method {invocation} timed out after {_timeout}");
            }
        }

        protected override async Task<TResult> InterceptAsync<TResult>(IInvocation invocation, IInvocationProceedInfo proceedInfo, Func<IInvocation, IInvocationProceedInfo, Task<TResult>> proceed)
        {
            var rootCt = InterceptionHelper.GetCancellationTokenOrDefault(invocation);

            
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(rootCt);
            InterceptionHelper.TryReplaceCancellationToken(invocation, cts.Token);
            async Task<TResult> To()
            {
                await Task.Delay(_timeout, rootCt).ConfigureAwait(false);
                cts.Cancel();
                throw new TimeoutException($"Method {invocation} timed out after {_timeout}");
            }

            try
            {
                return await await Task.WhenAny(
                        Task.Run(async () => await proceed(invocation, proceedInfo), cts.Token),
                        To())
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (rootCt.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Method {invocation} timed out after {_timeout}");
            }
        }
    }
}