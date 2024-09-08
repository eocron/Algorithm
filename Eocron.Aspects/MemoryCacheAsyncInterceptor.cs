using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Eocron.Aspects.Caching;
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

            if (_cache.TryGetValue(key, out object? tmp))
            {
                invocation.ReturnValue = ((AtomicLazy<object>)tmp).Value;
                return;
            }

            lock (_cache)
            {
                if (!_cache.TryGetValue(key, out tmp))
                {
                    tmp = new AtomicLazy<object>(() =>
                    {
                        invocation.Proceed();
                        return invocation.ReturnValue;
                    });
                    using var entry = _cache.CreateEntry(key);
                    _configureEntry(invocation.MethodInvocationTarget, invocation.Arguments, entry);
                    entry.SetValue(tmp);
                }
            }
            invocation.ReturnValue = ((AtomicLazy<object>)tmp).Value;
        }

        public void InterceptAsynchronous(IInvocation invocation)
        {
            invocation.Proceed();
        }

        public void InterceptAsynchronous<TResult>(IInvocation invocation)
        {
            var key = CreateKey(invocation);

            if (_cache.TryGetValue(key, out object? tmp))
            {
                invocation.ReturnValue = ((AsyncAtomicLazy<TResult>)tmp).Value();
                return;
            }

            lock (_cache)
            {
                if (!_cache.TryGetValue(key, out tmp))
                {
                    tmp = new AsyncAtomicLazy<TResult>(async () =>
                    {
                        invocation.Proceed();
                        var task = (Task<TResult>)invocation.ReturnValue;
                        return await task.ConfigureAwait(false);
                    });
                    using var entry = _cache.CreateEntry(key);
                    _configureEntry(invocation.MethodInvocationTarget, invocation.Arguments, entry);
                    entry.SetValue(tmp);
                }
            }
            invocation.ReturnValue = ((AsyncAtomicLazy<TResult>)tmp).Value();
        }
    }
}