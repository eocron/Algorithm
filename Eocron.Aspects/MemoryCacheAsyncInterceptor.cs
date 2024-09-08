using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;

namespace Eocron.Aspects
{
    public sealed class MemoryCacheAsyncInterceptor : IAsyncInterceptor
    {
        private readonly IMemoryCache _cache;
        private readonly Func<MethodInfo, object[], object> _keyProvider;
        private readonly Action<MethodInfo, object[], ICacheEntry> _configureEntry;
        private readonly bool _isGlobal;
        private readonly Guid _cacheId;

        public MemoryCacheAsyncInterceptor(IMemoryCache cache,
            Func<MethodInfo, object[], object> keyProvider,
            Action<MethodInfo, object[], ICacheEntry> configureEntry,
            bool isGlobal = false)
        {
            _cache = cache;
            _keyProvider = keyProvider;
            _configureEntry = configureEntry;
            _isGlobal = isGlobal;
            _cacheId = Guid.NewGuid();
        }

        private object CreateKey(IInvocation invocation)
        {
            var parts = new List<object>();
            if (!_isGlobal)
            {
                parts.Add(_cacheId);
            }
            parts.Add(invocation.MethodInvocationTarget);
            parts.Add(_keyProvider(invocation.MethodInvocationTarget, invocation.Arguments));
            return new CompoundKey(parts);
        }

        public void InterceptSynchronous(IInvocation invocation)
        {
            if (invocation.MethodInvocationTarget.ReturnType == typeof(void))
            {
                invocation.Proceed();
                return;
            }
            
            var key = CreateKey(invocation);
            var lazy = _cache.GetOrCreate(key, entry =>
            {
                var result = new Lazy<object>(() =>
                {
                    invocation.Proceed();
                    return invocation.ReturnValue;
                }, LazyThreadSafetyMode.ExecutionAndPublication);
                _configureEntry(invocation.MethodInvocationTarget, invocation.Arguments, entry);
                return result;
            });
            invocation.ReturnValue = lazy.Value;
        }

        public void InterceptAsynchronous(IInvocation invocation)
        {
            invocation.Proceed();
        }

        public void InterceptAsynchronous<TResult>(IInvocation invocation)
        {
            var key = CreateKey(invocation);
            invocation.ReturnValue = _cache.GetOrCreateAsync(key, async entry =>
            {
                _configureEntry(invocation.MethodInvocationTarget, invocation.Arguments, entry);
                invocation.Proceed();
                var task = (Task<TResult>)invocation.ReturnValue;
                return await task.ConfigureAwait(false);
            });
        }
    }
}