using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Eocron.DependencyInjection
{
    public static partial class ServiceCollectionServiceExtensions
    {
        public static IServiceCollection Add(this IServiceCollection services, ServiceDescriptor descriptor, DecoratorChain chain)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            ArgumentNullException.ThrowIfNull(chain);

            if (chain.Items.Count == 0)
            {
                services.Add(descriptor);
                return services;
            }

            var implementationDescriptor = CloneWithNewKey(descriptor);
            services.Add(implementationDescriptor);
            var prevKey = implementationDescriptor.ServiceKey;
            for (var i = chain.Items.Count - 1; i >= 0; i--)
            {
                var d = chain.Items[i];
                var isLast = i == 0;
                var pk = prevKey;
                if (isLast)
                {
                    if (descriptor.IsKeyedService)
                    {
                        services.Add(new ServiceDescriptor(
                            descriptor.ServiceType, 
                            descriptor.ServiceKey, 
                            (sp, _) => d(sp, sp.GetRequiredKeyedService(descriptor.ServiceType, pk)),
                            descriptor.Lifetime));
                    }
                    else
                    {
                        services.Add(new ServiceDescriptor(
                            descriptor.ServiceType,
                            sp => d(sp, sp.GetRequiredKeyedService(descriptor.ServiceType, pk)),
                            descriptor.Lifetime));
                    }
                }
                else
                {
                    var nextKey = GenerateKey();
                    services.Add(new ServiceDescriptor(
                        descriptor.ServiceType, 
                        nextKey, 
                        (sp, _) => d(sp, sp.GetRequiredKeyedService(descriptor.ServiceType, pk)),
                        descriptor.Lifetime));
                    prevKey = nextKey;
                }
            }

            return services;
        }

        private static string GenerateKey()
        {
            return Interlocked.Increment(ref _counter).ToString("000000") + "_decorator";
        }

        private static long _counter = 0;

        private static ServiceDescriptor CloneWithNewKey(ServiceDescriptor descriptor)
        {
            var newKey = GenerateKey();
            if (descriptor.IsKeyedService)
            {
                if (descriptor.KeyedImplementationInstance != null)
                {
                    return new ServiceDescriptor(descriptor.ServiceType, newKey, (_,_)=> descriptor.KeyedImplementationInstance, descriptor.Lifetime);
                }
                if (descriptor.KeyedImplementationFactory != null)
                {
                    return new ServiceDescriptor(descriptor.ServiceType, newKey, (sp,_)=> descriptor.KeyedImplementationFactory(sp, descriptor.ServiceKey), descriptor.Lifetime);
                }
                return new ServiceDescriptor(descriptor.ServiceType, newKey, descriptor.KeyedImplementationType, descriptor.Lifetime);
            }

            if (descriptor.ImplementationInstance != null)
            {
                return new ServiceDescriptor(descriptor.ServiceType, newKey, (_,_)=> descriptor.ImplementationInstance, descriptor.Lifetime);
            }
            if (descriptor.ImplementationFactory != null)
            {
                return new ServiceDescriptor(descriptor.ServiceType, newKey, (sp,_)=> descriptor.ImplementationFactory(sp), descriptor.Lifetime);
            }
            return new ServiceDescriptor(descriptor.ServiceType, newKey, descriptor.ImplementationType, descriptor.Lifetime);
        }
        
        private static DecoratorChain ToChain(Action<DecoratorChain> chainBuilder)
        {
            var chain = new DecoratorChain();
            chainBuilder(chain);
            return chain;
        }
    }
}