using System;
using Castle.DynamicProxy;
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
    }
}