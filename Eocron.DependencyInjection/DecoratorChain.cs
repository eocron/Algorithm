using System.Collections.Generic;

namespace Eocron.DependencyInjection
{
    public sealed class DecoratorChain
    {
        private readonly List<DecoratorDelegate> _items = new();
        public IReadOnlyList<DecoratorDelegate> Items => _items;

        public DecoratorChain Add(DecoratorDelegate decorator)
        {
            _items.Add(decorator);
            return this;
        }
    }
}