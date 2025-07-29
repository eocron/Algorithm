using System;
using Microsoft.Extensions.DependencyInjection;

namespace Eocron.DependencyInjection
{
    public delegate object DecoratorDelegate(IServiceProvider provider, string keyPrefix, object instance, ServiceLifetime lifetime);
}