using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Mega.Platform.Core.Client;

public static class ClientMqttSetup
{
    /// <summary>
    /// Adds the necessary services for remote capability using MQTT to the service collection.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static void AddMqttCapability(this IServiceCollection services, IConfiguration configuration)
    {
        Throw.IfNull(configuration.GetSection(nameof(DeviceDescriptor)).Get<DeviceDescriptor>(), "Device descriptor is not configured.");
        Throw.IfNull(configuration.GetSection(nameof(MqttServerConnectionOptions)).Get<MqttServerConnectionOptions>(), "MQTT server connection options are not configured.");

        services.Configure<MqttServerConnectionOptions>(configuration.GetSection(nameof(MqttServerConnectionOptions)));
        services.Configure<DeviceDescriptor>(configuration.GetSection(nameof(DeviceDescriptor)));
        services.AddMqttClient(configuration);
        services.AddSingleton<IDevice, DefaultDevice>();
        services.AddSingleton<IMqttClientWrapper, MqttClientWrapper>();
        services.AddSingleton<IObjectFactory, ObjectFactory>();
        services.AddSingleton<IMqttMessagePublisher, MqttMessagePublisher>();
        services.AddSingleton<IClientMqttApplicationMessageListener, DefaultClientMqttApplicationMessageListener>();
        services.AddSingleton<IAsyncTaskWaiter, AsyncTaskWaiter>();
    }
}
