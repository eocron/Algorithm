using System;
using System.Collections.Generic;

namespace Eocron.DependencyInjection
{
    public sealed class DecoratorChain
    {
        private readonly List<Decorator> _items = new();
        public IReadOnlyList<Decorator> Items => _items;
        public Type ServiceType { get; }

        public DecoratorChain(Type serviceType)
        {
            ServiceType = serviceType;
        }
        public DecoratorChain Add(DecoratorDelegate decorator, DecoratorConfiguratorDelegate configurator = null)
        {
            _items.Add(new Decorator()
            {
                Provider = decorator,
                Configurator = configurator
            });
            return this;
        }
    }

    public class Decorator
    {
        public DecoratorDelegate Provider { get; init; }
        
        public DecoratorConfiguratorDelegate Configurator { get; init; }
    }
}