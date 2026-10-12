using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mega.Platform.Core.Client;

public static class ClientMqttSetup
{
    /// <summary>
    /// Adds the necessary services for remote capability using MQTT to the service collection.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static void AddRemoteCapability(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMqttClient(configuration);
        services.AddSingleton<IMqttClientWrapper, MqttClientWrapper>();
        services.AddSingleton<IObjectFactory, ObjectFactory>();
        services.AddSingleton<IMqttMessagePublisher, MqttMessagePublisher>();
        services.AddSingleton<IClientMqttApplicationMessageListener, DefaultClientMqttApplicationMessageListener>();
        services.AddSingleton<IAsyncTaskWaiter, AsyncTaskWaiter>();
    }
}
