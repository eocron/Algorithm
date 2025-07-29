using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;

namespace Eocron.DependencyInjection.Interceptors.Retry
{
    public sealed class RetryAsyncInterceptor : IAsyncInterceptor
    {
        private readonly Func<int, Exception, bool> _exceptionPredicate;
        private readonly Func<int, Exception, TimeSpan> _retryIntervalProvider;
        private readonly ILogger _logger;

        public RetryAsyncInterceptor(
            Func<int, Exception, bool> exceptionPredicate, 
            Func<int, Exception, TimeSpan> retryIntervalProvider,
            ILogger logger)
        {
            _exceptionPredicate = exceptionPredicate ?? throw new ArgumentNullException(nameof(exceptionPredicate));
            _retryIntervalProvider = retryIntervalProvider ?? throw new ArgumentNullException(nameof(retryIntervalProvider));
            _logger = logger;
        }

        public void InterceptSynchronous(IInvocation invocation)
        {
            ExecuteSync(invocation);
        }

        public void InterceptAsynchronous(IInvocation invocation)
        {
            var proceedInfo = invocation.CaptureProceedInfo();
            invocation.ReturnValue = ExecuteAsync(invocation, proceedInfo);
        }

        public void InterceptAsynchronous<TResult>(IInvocation invocation)
        {
            var proceedInfo = invocation.CaptureProceedInfo();
            invocation.ReturnValue = ExecuteAsync<TResult>(invocation, proceedInfo);
        }
        
        private void ExecuteSync(IInvocation invocation)
        {
            var ct = InterceptionHelper.GetCancellationTokenOrDefault(invocation);
            var iteration = 0;
            while (true)
            {
                LogBegin(invocation, iteration);
                try
                {
                    invocation.Proceed();
                    LogSuccess(invocation, iteration);
                    return;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    LogCancel(invocation, iteration);
                    throw;
                }
                catch (Exception ex)
                {
                    LogFail(invocation, iteration, ex);
                    iteration++;
                    if (!_exceptionPredicate(iteration, ex) || ct.IsCancellationRequested)
                    {
                        LogCancel(invocation, iteration);
                        throw;
                    }

                    var retryInterval = _retryIntervalProvider(iteration, ex);
                    if (retryInterval == TimeSpan.Zero)
                    {
                        continue;
                    }

                    InterceptionHelper.SafeDelay(retryInterval, ct);

                    if (!ct.IsCancellationRequested) continue;
                    LogCancel(invocation, iteration);
                    throw;
                }
            }
        }

        private async Task ExecuteAsync(IInvocation invocation, IInvocationProceedInfo proceedInfo)
        {
            var ct = InterceptionHelper.GetCancellationTokenOrDefault(invocation);
            var iteration = 0;
            while (true)
            {
                LogBegin(invocation, iteration);
                try
                {
                    proceedInfo.Invoke();
                    var task = (Task)invocation.ReturnValue;
                    await task.ConfigureAwait(false);
                    LogSuccess(invocation, iteration);
                    return;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    LogCancel(invocation, iteration);
                    throw;
                }
                catch (Exception ex)
                {
                    LogFail(invocation, iteration, ex);
                    iteration++;
                    if (!_exceptionPredicate(iteration, ex) || ct.IsCancellationRequested)
                    {
                        LogCancel(invocation, iteration);
                        throw;
                    }

                    var retryInterval = _retryIntervalProvider(iteration, ex);
                    if (retryInterval == TimeSpan.Zero)
                    {
                        continue;
                    }

                    await InterceptionHelper.SafeDelayAsync(retryInterval, ct).ConfigureAwait(false);

                    if (!ct.IsCancellationRequested) continue;
                    LogCancel(invocation, iteration);
                    throw;
                }
            }
        }

        private async Task<T> ExecuteAsync<T>(IInvocation invocation, IInvocationProceedInfo proceedInfo)
        {
            var ct = InterceptionHelper.GetCancellationTokenOrDefault(invocation);
            var iteration = 0;
            while (true)
            {
                LogBegin(invocation, iteration);
                try
                {
                    proceedInfo.Invoke();
                    var task = (Task<T>)invocation.ReturnValue;
                    var result = await task.ConfigureAwait(false);
                    LogSuccess(invocation, iteration);
                    return result;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    LogCancel(invocation, iteration);
                    throw;
                }
                catch (Exception ex)
                {
                    LogFail(invocation, iteration, ex);
                    iteration++;
                    if (!_exceptionPredicate(iteration, ex) || ct.IsCancellationRequested)
                    {
                        LogCancel(invocation, iteration);
                        throw;
                    }

                    var retryInterval = _retryIntervalProvider(iteration, ex);
                    if (retryInterval == TimeSpan.Zero)
                    {
                        continue;
                    }

                    await InterceptionHelper.SafeDelayAsync(retryInterval, ct).ConfigureAwait(false);

                    if (!ct.IsCancellationRequested) continue;
                    LogCancel(invocation, iteration);
                    throw;
                }
            }
        }
        
        private void LogBegin(IInvocation invocation, int iteration)
        {
            if (iteration == 0)
            {
                return;
            }
            _logger?.LogTrace("Retrying {invocation}. Attempt: {attempt}", invocation.Method.Name, iteration);
        }
        
        private void LogSuccess(IInvocation invocation, int iteration)
        {
            if (iteration == 0)
            {
                return;
            }
            _logger?.LogTrace("Retry success {invocation}! Attempt: {attempt}", invocation.Method.Name, iteration);
        }
        
        
        private void LogFail(IInvocation invocation, int iteration, Exception ex)
        {
            if (iteration == 0)
            {
                _logger?.LogError(ex, "Error on calling {invocation}", invocation.Method.Name);
                return;
            }
            _logger?.LogError(ex, "Error on calling {invocation}. Attempt: {attempt}", invocation.Method.Name, iteration);
        }
        
        private void LogCancel(IInvocation invocation, int iteration)
        {
            if (iteration == 0)
            {
                _logger?.LogTrace("Retrying of {invocation} stopped on reaching stop condition", invocation.Method.Name);
                return;
            }
            _logger?.LogTrace("Retrying of {invocation} stopped on reaching stop condition. Attempt: {attempt}", invocation.Method.Name, iteration);
        }
    }
}