using Microsoft.Extensions.DependencyInjection;

namespace Mega.Platform.Core;

public static class ServiceProviderExtensions
{
    public static bool TryGetService<T>(this IServiceProvider serviceProvider, out T? serviceInstance)
    {
        serviceInstance = serviceProvider.GetService<T>();
        return serviceInstance != null;
    }
}
