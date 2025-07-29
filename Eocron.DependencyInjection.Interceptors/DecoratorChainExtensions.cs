using System;
using System.Reflection;
using Castle.DynamicProxy;
using Eocron.DependencyInjection.Interceptors.Caching;
using Eocron.DependencyInjection.Interceptors.Locking;
using Eocron.DependencyInjection.Interceptors.Logging;
using Eocron.DependencyInjection.Interceptors.Retry;
using Eocron.DependencyInjection.Interceptors.Timeout;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Eocron.DependencyInjection.Interceptors
{
    public static class DecoratorChainExtensions
    {
        public static DecoratorChain AddInterceptor(this DecoratorChain decoratorChain,
            Func<IServiceProvider, IAsyncInterceptor> interceptorFactory)
        {
            decoratorChain.Add((sp, keyPrefix, instance, lifetime) => InterceptionHelper.CreateProxy(decoratorChain.ServiceType, instance, interceptorFactory(sp)));
            return decoratorChain;
        }
        
        public static DecoratorChain AddInterceptor(this DecoratorChain decoratorChain,
            Func<IServiceProvider, string, ServiceLifetime, IAsyncInterceptor> interceptorFactory,
            DecoratorConfiguratorDelegate interceptorConfigurator)
        {
            decoratorChain.Add((sp, keyPrefix, instance, lifetime) => InterceptionHelper.CreateProxy(decoratorChain.ServiceType, instance, interceptorFactory(sp, keyPrefix, lifetime)), interceptorConfigurator);
            return decoratorChain;
        }

        public static DecoratorChain AddLock(this DecoratorChain decoratorChain,
            Func<IServiceProvider, ILockProvider> lockProvider)
        {
            decoratorChain.AddInterceptor(sp =>
                new LockAsyncInterceptor(
                    lockProvider(sp),
                    false));
            return decoratorChain;
        }

        public static DecoratorChain AddSemaphoreSlimLock(this DecoratorChain decoratorChain, int initialCount = 1)
        {
            decoratorChain.AddInterceptor((sp, keyPrefix, lifetime) =>
                new LockAsyncInterceptor(sp.GetRequiredKeyedService<ILockProvider>(keyPrefix), lifetime == ServiceLifetime.Transient),
                (services, keyPrefix, lifetime) =>
                {
                    services.Add(new ServiceDescriptor(typeof(ILockProvider), keyPrefix, (_,_)=> new SemaphoreSlimLockProvider(initialCount), lifetime));
                });
            return decoratorChain;
        }
        
        public static DecoratorChain AddMonitorLock(this DecoratorChain decoratorChain)
        {
            decoratorChain.AddInterceptor((sp) =>
                    new LockAsyncInterceptor(new MonitorLockProvider(), false));
            return decoratorChain;
        }

        public static DecoratorChain AddTracing(this DecoratorChain decoratorChain)
        {
            decoratorChain.AddInterceptor(sp =>
                new LoggingAsyncInterceptor(
                    sp.GetService<ILoggerFactory>().CreateLogger(decoratorChain.ServiceType.FullName),
                    LogLevel.Trace,
                    LogLevel.Error));
            return decoratorChain;
        }

        public static DecoratorChain AddTimeout(this DecoratorChain decoratorChain, TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
            {
                return decoratorChain;
            }

            decoratorChain.AddInterceptor(_ => new TimeoutAsyncInterceptor(timeout));
            return decoratorChain;
        }
        
        public static DecoratorChain AddRetry(this DecoratorChain decoratorChain, 
            Func<int, Exception, bool> exceptionPredicate, 
            Func<int, Exception, TimeSpan> retryIntervalProvider)
        {
            decoratorChain.AddInterceptor(sp => new RetryAsyncInterceptor(exceptionPredicate,
                retryIntervalProvider,
                sp.GetService<ILoggerFactory>()?.CreateLogger(decoratorChain.ServiceType.Name)));
            return decoratorChain;
        }
        
        public static DecoratorChain AddConstantBackoff(this DecoratorChain decoratorChain, 
            int maxAttempts, 
            TimeSpan retryInterval,
            bool jittered = false,
            Func<Exception, bool> isRetryable = null)
        {
            return decoratorChain.AddRetry(
                (c, ex) => c <= maxAttempts && (isRetryable?.Invoke(ex) ?? true),
                (_, _) => ConstantBackoff.Calculate(StaticRandom.Value, retryInterval, jittered));
        }
        
        public static DecoratorChain AddExponentialBackoff(this DecoratorChain decoratorChain, 
            int maxAttempts,
            TimeSpan minPropagationDuration, 
            TimeSpan maxPropagationDuration,
            bool jittered = true,
            Func<Exception, bool> isRetryable = null)
        {
            return decoratorChain.AddRetry(
                (c, ex) => c <= maxAttempts && (isRetryable?.Invoke(ex) ?? true),
                (c, _) => CorrelatedExponentialBackoff.Calculate(StaticRandom.Value, c, minPropagationDuration, maxPropagationDuration, jittered));
        }
        
        public static DecoratorChain AddCache(this DecoratorChain decoratorChain,
            Func<MethodInfo, object[], object> keyProvider = null)
        {
            decoratorChain.AddInterceptor(sp => new MemoryCacheAsyncInterceptor(sp.GetRequiredService<IMemoryCache>(),
                keyProvider ?? KeyProviderHelper.AllExceptCancellationToken,
                (_,_,_)=> {}));
            return decoratorChain;
        }
        
        public static DecoratorChain AddSlidingTimeoutCache(this DecoratorChain decoratorChain, 
            TimeSpan cacheDuration,
            Func<MethodInfo, object[], object> keyProvider = null)
        {
            if (cacheDuration <= TimeSpan.Zero)
                return decoratorChain;
            
            decoratorChain.AddInterceptor(sp => new MemoryCacheAsyncInterceptor(sp.GetRequiredService<IMemoryCache>(),
                keyProvider ?? KeyProviderHelper.AllExceptCancellationToken,
                (_,_,ce)=> ce.SetSlidingExpiration(cacheDuration)));
            return decoratorChain;
        }
        
        public static DecoratorChain AddAbsoluteTimeoutCache(this DecoratorChain decoratorChain, 
            TimeSpan cacheDuration,
            Func<MethodInfo, object[], object> keyProvider = null)
        {
            if (cacheDuration <= TimeSpan.Zero)
                return decoratorChain;
            
            decoratorChain.AddInterceptor(sp => new MemoryCacheAsyncInterceptor(sp.GetRequiredService<IMemoryCache>(),
                keyProvider ?? KeyProviderHelper.AllExceptCancellationToken,
                (_,_,ce)=> ce.SetAbsoluteExpiration(cacheDuration)));
            return decoratorChain;
        }
    }
}