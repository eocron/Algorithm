using System;
using System.Reflection;
using Castle.DynamicProxy;
using Eocron.DependencyInjection.Interceptors.Caching;
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
            decoratorChain.Add((sp, instance) => InterceptionHelper.CreateProxy(instance, interceptorFactory(sp)));
            return decoratorChain;
        }
        
        public static DecoratorChain AddInterceptor(this DecoratorChain decoratorChain,
            IAsyncInterceptor interceptor)
        {
            decoratorChain.Add((sp, instance) => InterceptionHelper.CreateProxy(instance, interceptor));
            return decoratorChain;
        }

        public static DecoratorChain AddTracing(this DecoratorChain decoratorChain)
        {
            decoratorChain.AddInterceptor((sp) =>
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

            decoratorChain.AddInterceptor(new TimeoutAsyncInterceptor(timeout));
            return decoratorChain;
        }
        
        public static DecoratorChain AddRetry(this DecoratorChain decoratorChain, 
            Func<int, Exception, bool> exceptionPredicate, 
            Func<int, Exception, TimeSpan> retryIntervalProvider)
        {
            decoratorChain.AddInterceptor((sp) => new RetryUntilConditionAsyncInterceptor(exceptionPredicate,
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
                (c, _) => ConstantBackoff.Calculate(StaticRandom.Value, retryInterval, jittered));
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
        
        public static DecoratorChain AddSlidingTimeoutCache(this DecoratorChain decoratorChain, 
            Func<MethodInfo, object[], object> keyProvider, 
            TimeSpan cacheDuration)
        {
            if (cacheDuration <= TimeSpan.Zero)
                return decoratorChain;
            
            decoratorChain.AddInterceptor((sp) => new MemoryCacheAsyncInterceptor(sp.GetRequiredService<IMemoryCache>(),
                keyProvider,
                (_,_,ce)=> ce.SetSlidingExpiration(cacheDuration)));
            return decoratorChain;
        }
        
        public static DecoratorChain AddTimeoutCache(this DecoratorChain decoratorChain, 
            Func<MethodInfo, object[], object> keyProvider, 
            TimeSpan cacheDuration)
        {
            if (cacheDuration <= TimeSpan.Zero)
                return decoratorChain;
            
            decoratorChain.AddInterceptor((sp) => new MemoryCacheAsyncInterceptor(sp.GetRequiredService<IMemoryCache>(),
                keyProvider,
                (_,_,ce)=> ce.SetAbsoluteExpiration(cacheDuration)));
            return decoratorChain;
        }
    }
}