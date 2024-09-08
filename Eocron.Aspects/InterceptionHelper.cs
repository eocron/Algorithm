using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Eocron.Aspects;

public static class InterceptionHelper
{
    public static CancellationToken? TryGetCancellationToken(IInvocation invocation)
    {
        return (CancellationToken?)invocation.Arguments.SingleOrDefault(x => x is CancellationToken);
    }

    public static async Task SafeDelay(TimeSpan delay, CancellationToken ct)
    {
        try
        {
            await Task.Delay(delay, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            //ignore
        }
    }

    public static T CreateProxy<T>(T target, IAsyncInterceptor interceptor) where T : class
    {
        ProxyGenerator generator = new ProxyGenerator();
        return generator.CreateInterfaceProxyWithTargetInterface<T>(target, interceptor.ToInterceptor());
    }
}