using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Eocron.Sharding
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection Replace<TInterface, TImplementation>(
            this IServiceCollection collection, Func<IServiceProvider, TInterface, TImplementation> replacer)
            where TImplementation : TInterface
        {
            var found = collection.Single(x => x.ServiceType == typeof(TInterface));
            collection.Remove(found);
            collection.Add(new ServiceDescriptor(typeof(TImplementation), sp => replacer(sp, (TInterface)found.ImplementationFactory(sp)), found.Lifetime));
            collection.Add(new ServiceDescriptor(typeof(TInterface), sp => sp.GetRequiredService<TImplementation>(), found.Lifetime));
            return collection;
        }

        public static IServiceCollection Replace<TInterface>(
            this IServiceCollection collection, Func<IServiceProvider, TInterface, TInterface> replacer)
        {
            var found = collection.Single(x => x.ServiceType == typeof(TInterface));
            collection.Remove(found);
            collection.Add(new ServiceDescriptor(found.ServiceType, sp => replacer(sp, (TInterface)found.ImplementationFactory(sp)), found.Lifetime));
            return collection;
        }
    }
}