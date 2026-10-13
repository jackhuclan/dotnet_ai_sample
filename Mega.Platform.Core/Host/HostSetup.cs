using Mega.Platform.Core.Client;
using Mega.Platform.Core.Host.Agent;
using Mega.Platform.Core.Host.Command;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;
using MQTTnet;

namespace Mega.Platform.Core.Host;

public static class HostSetup
{
    public static void AddHost(this IServiceCollection services, IConfiguration configuration)
    {
        Throw.IfNull(configuration.GetSection(nameof(DeviceDescriptor)).Get<DeviceDescriptor>(), "Device descriptor is not configured.");
        Throw.IfNull(configuration.GetSection(nameof(MqttServerConnectionOptions)).Get<MqttServerConnectionOptions>(), "MQTT server connection options are not configured.");

        services.Configure<MqttServerConnectionOptions>(configuration.GetSection(nameof(MqttServerConnectionOptions)));
        services.Configure<DeviceDescriptor>(configuration.GetSection(nameof(DeviceDescriptor)));

        services.AddMqttClient();
        services.AddHostedService<HostMqttClient>();
        services.AddSingleton<IDeviceServiceInvoker, HostDeviceServiceInvoker>();
        services.AddSingleton<IMqttMessageDispatcher, MqttMessageDispatcher>();
        services.AddSingleton<IMqttMessageHandlerFactory, MqttMessageHandlerFactory>();
        services.AddSingleton<IDeviceServiceInvoker, HostDeviceServiceInvoker>();
        services.AddSingleton<IMqttMessagePublisher, MqttMessagePublisher>();
        services.AddSingleton<IDeviceManager, DeviceManager>();
        services.AddSingleton<IObjectFactory, ObjectFactory>();
        services.AddSingleton<IAsyncTaskWaiter, AsyncTaskWaiter>();
        services.AddSingleton<IAgentManager, AgentManager>();
        services.AddSingleton<ISessionManager, SessionManager>();
        services.AddSingleton<CameraCommandSet>();
    }

    private static void AddMqttClient(this IServiceCollection services)
    {
        var mqttFactory = new MqttClientFactory();
        IMqttClient mqttClient = mqttFactory.CreateMqttClient();
        services.AddSingleton(mqttClient);
    }
}
