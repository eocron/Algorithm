using Microsoft.Extensions.DependencyInjection;

namespace Eocron.DependencyInjection
{
    public delegate void DecoratorConfiguratorDelegate(IServiceCollection services, string keyPrefix, ServiceLifetime lifetime);
}