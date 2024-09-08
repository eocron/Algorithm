using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;

namespace Eocron.Aspects
{
    public sealed class RetryUntilConditionAsyncInterceptor : IAsyncInterceptor
    {
        private readonly Func<int, Exception, bool> _exceptionPredicate;
        private readonly Func<int, Exception, TimeSpan> _retryIntervalProvider;
        private readonly ILogger _logger;

        public RetryUntilConditionAsyncInterceptor(
            Func<int, Exception, bool> exceptionPredicate, 
            Func<int, Exception, TimeSpan> retryIntervalProvider,
            ILogger logger)
        {
            _exceptionPredicate = exceptionPredicate;
            _retryIntervalProvider = retryIntervalProvider;
            _logger = logger;
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
            var totalTries = 0;
            while (true)
            {
                totalTries++;
                _logger?.LogTrace("Trying to invoke {invocation}. Total tries: {totalTries}", invocation.Method.Name,
                    totalTries);
                try
                {
                    invocation.Proceed();
                    return;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to invoke {invocation}. Total tries: {totalTries}", invocation.Method.Name,
                        totalTries);
                    if (!_exceptionPredicate(totalTries, ex))
                    {
                        _logger?.LogTrace("Retrying of {invocation} stopped on reaching stop condition. Total tries: {totalTries}", invocation.Method.Name,
                            totalTries);
                        throw;
                    }

                    var retryInterval = _retryIntervalProvider(totalTries, ex);
                    if (retryInterval == TimeSpan.Zero)
                    {
                        continue;
                    }
                    Thread.Sleep(retryInterval);
                }
            }
        }

        private async Task ExecuteAsync(IInvocation invocation)
        {
            var ct = InterceptionHelper.GetCancellationTokenOrDefault(invocation);
            var totalTries = 0;
            while (true)
            {
                totalTries++;
                _logger?.LogTrace("Trying to invoke {invocation}. Total tries: {totalTries}", invocation.Method.Name,
                    totalTries);
                try
                {
                    invocation.Proceed();
                    var task = (Task)invocation.ReturnValue; 
                    await task.ConfigureAwait(false);
                    return;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    _logger?.LogTrace("Retrying of {invocation} stopped on reaching cancellation. Total tries: {totalTries}", invocation.Method.Name,
                        totalTries);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger?.LogTrace(ex, "Failed to invoke {invocation}. Total tries: {totalTries}", invocation.Method.Name,
                        totalTries);
                    if (!_exceptionPredicate(totalTries, ex))
                    {
                        _logger?.LogTrace("Retrying of {invocation} stopped on reaching stop condition. Total tries: {totalTries}", invocation.Method.Name,
                            totalTries);
                        throw;
                    }
                    
                    if (ct.IsCancellationRequested)
                    {
                        _logger?.LogTrace("Retrying of {invocation} stopped on reaching cancellation. Total tries: {totalTries}", invocation.Method.Name,
                            totalTries);
                        throw;
                    }

                    var retryInterval = _retryIntervalProvider(totalTries, ex);
                    if (retryInterval == TimeSpan.Zero)
                    {
                        continue;
                    }
                    
                    await InterceptionHelper.SafeDelay(retryInterval, ct);
                    
                    if (!ct.IsCancellationRequested)
                    {
                        continue;
                    }
                    _logger?.LogTrace("Retrying of {invocation} cancelled. Total tries: {totalTries}",
                        invocation.Method.Name,
                        totalTries);
                    throw;
                }
            }
        }

        private async Task<T> ExecuteAsync<T>(IInvocation invocation)
        {
            var ct = InterceptionHelper.GetCancellationTokenOrDefault(invocation);
            var totalTries = 0;
            while (true)
            {
                totalTries++;
                _logger?.LogTrace("Trying to invoke {invocation}. Total tries: {totalTries}", invocation.Method.Name,
                    totalTries);
                try
                {
                    invocation.Proceed();
                    var task = (Task<T>)invocation.ReturnValue;
                    return await task.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    _logger?.LogTrace("Retrying of {invocation} stopped on reaching cancellation. Total tries: {totalTries}", invocation.Method.Name,
                        totalTries);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to invoke {invocation}. Total tries: {totalTries}", invocation.Method.Name,
                        totalTries);
                    if (!_exceptionPredicate(totalTries, ex))
                    {
                        _logger?.LogTrace("Retrying of {invocation} stopped on reaching stop condition. Total tries: {totalTries}", invocation.Method.Name,
                            totalTries);
                        throw;
                    }

                    if (ct.IsCancellationRequested)
                    {
                        _logger?.LogTrace("Retrying of {invocation} stopped on reaching cancellation. Total tries: {totalTries}", invocation.Method.Name,
                            totalTries);
                        throw;
                    }

                    var retryInterval = _retryIntervalProvider(totalTries, ex);
                    if (retryInterval == TimeSpan.Zero)
                    {
                        continue;
                    }

                    await InterceptionHelper.SafeDelay(retryInterval, ct);

                    if (!ct.IsCancellationRequested)
                    {
                        continue;
                    }
                    _logger?.LogTrace("Retrying of {invocation} cancelled. Total tries: {totalTries}",
                        invocation.Method.Name,
                        totalTries);
                    throw;
                }
            }
        }
    }
}