using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Eocron.DependencyInjection.Interceptors;

public static class InterceptionHelper
{
    public static CancellationToken GetCancellationTokenOrDefault(IInvocation invocation)
    {
        if (TryGetCancellationTokenIndex(invocation, out var idx))
        {
            return (CancellationToken)invocation.GetArgumentValue(idx);
        }
        return CancellationToken.None;
    }

    public static void TryReplaceCancellationToken(IInvocation invocation, CancellationToken newCt)
    {
        if (TryGetCancellationTokenIndex(invocation, out var idx))
        {
            invocation.SetArgumentValue(idx, newCt);
        }
    }

    private static bool TryGetCancellationTokenIndex(IInvocation invocation, out int index)
    {
        index = -1;
        for(var i = 0; i < invocation.Arguments.Length; i++)
        {
            if (invocation.Arguments[i] is CancellationToken)
            {
                index = i;
                return true;
            }
        }
        return false;
    }

    public static void SafeDelay(TimeSpan delay, CancellationToken ct = default)
    {
        Thread.Sleep(delay);
    }

    public static async Task SafeDelayAsync(TimeSpan delay, CancellationToken ct = default)
    {
        try
        {
            await Task.Delay(delay, ct).ConfigureAwait(false);
        }
        catch
        {
            //ignore
        }
    }

    public static T CreateProxy<T>(T target, params IAsyncInterceptor[] interceptors) where T : class
    {
        ProxyGenerator generator = new ProxyGenerator();
        return generator.CreateInterfaceProxyWithTargetInterface<T>(target, interceptors.Select(x=> x.ToInterceptor()).ToArray());
    }
    
    public static object CreateProxy(Type interfaceType, object target, params IAsyncInterceptor[] interceptors)
    {
        ProxyGenerator generator = new ProxyGenerator();
        return generator.CreateInterfaceProxyWithTargetInterface(interfaceType, target, interceptors.Select(x=> x.ToInterceptor()).ToArray());
    }
}