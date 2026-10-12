using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Mega.Platform.Core;

public class ObjectFactory : IObjectFactory
{
    private readonly IServiceProvider _serviceProvider;
    private ConcurrentDictionary<Type, object> _objects = new();

    public ObjectFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public T CreateObject<T>(params object[] parameters)
    {
        if (typeof(T).IsAbstract) throw new ArgumentException(nameof(T));
        return (T)ActivatorUtilities.CreateInstance(_serviceProvider, typeof(T), parameters);
    }

    public T GetOrCreate<T>(params object[] parameters)
    {
        if (_objects.TryGetValue(typeof(T), out var obj))
        {
            return (T)obj;
        }
        else if (_serviceProvider.TryGetService<T>(out var serviceInstance)
            && serviceInstance != null)
        {
            _objects.TryAdd(typeof(T), serviceInstance);
            return serviceInstance;
        }
        else
        {
            var obj2 = CreateObject<T>(parameters);
            _objects.TryAdd(typeof(T), obj2);
            return obj2;
        }
    }
}
