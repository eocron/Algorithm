using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;

namespace Eocron.DependencyInjection.Interceptors.Logging
{
    public sealed class LoggingAsyncInterceptor : IAsyncInterceptor
    {
        private readonly ILogger _logger;
        private readonly LogLevel _onTrace;
        private readonly LogLevel _onError;

        public LoggingAsyncInterceptor(
            ILogger logger,
            LogLevel onTrace,
            LogLevel onError)
        {
            _logger = logger;
            _onTrace = onTrace;
            _onError = onError;
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
            var sw = Stopwatch.StartNew();
            _logger?.Log(_onTrace, "Call {invocation}", invocation.Method.Name);
            try
            {
                invocation.Proceed();
                _logger?.Log(_onTrace, "Call {invocation} ended. Elapsed: {elapsed}", invocation.Method.Name, sw.Elapsed);
                return;
            }
            catch (Exception ex)
            {
                _logger?.Log(_onError, ex, "Failed to invoke {invocation}. Elapsed: {elapsed}", invocation.Method.Name, sw.Elapsed);
                throw;
            }
        }

        private async Task ExecuteAsync(IInvocation invocation)
        {
            var sw = Stopwatch.StartNew();
            _logger?.Log(_onTrace, "Call {invocation}", invocation.Method.Name);
            try
            {
                invocation.Proceed();
                var task = (Task)invocation.ReturnValue; 
                await task.ConfigureAwait(false);
                _logger?.Log(_onTrace, "Call {invocation} ended. Elapsed: {elapsed}", invocation.Method.Name, sw.Elapsed);
            }
            catch (Exception ex)
            {
                _logger?.Log(_onError, ex, "Failed to invoke {invocation}. Elapsed: {elapsed}", invocation.Method.Name, sw.Elapsed);
                throw;
            }
        }

        private async Task<T> ExecuteAsync<T>(IInvocation invocation)
        {
            var sw = Stopwatch.StartNew();
            _logger?.Log(_onTrace, "Call {invocation}", invocation.Method.Name);
            try
            {
                invocation.Proceed();
                var task = (Task<T>)invocation.ReturnValue;
                var result = await task.ConfigureAwait(false);
                _logger?.Log(_onTrace, "Call {invocation} ended. Elapsed: {elapsed}", invocation.Method.Name, sw.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.Log(_onError, ex, "Failed to invoke {invocation}. Elapsed: {elapsed}", invocation.Method.Name, sw.Elapsed);
                throw;
            }
        }
    }
}