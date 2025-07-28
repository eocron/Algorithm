using System;

namespace Eocron.DependencyInjection
{
    public delegate object DecoratorDelegate(IServiceProvider provider, object instance);
}