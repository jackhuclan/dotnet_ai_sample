using Mega.Platform.Core.Host.Command;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;

namespace Mega.Platform.Core.Host;

public static class HostSetup
{
    public static void AddHost(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MqttServerConnectionOptions>(configuration.GetSection(nameof(MqttServerConnectionOptions)));
        services.AddMqttClient();
        services.AddHostedService<HostMqttClient>();
        services.AddSingleton<IMqttMessageDispatcher, MqttMessageDispatcher>();
        services.AddSingleton<IMqttMessageHandlerFactory, MqttMessageHandlerFactory>();
        services.AddSingleton<IDeviceServiceInvoker, HostDeviceServiceInvoker>();
        services.AddSingleton<IMqttMessagePublisher, MqttMessagePublisher>();
        services.AddSingleton<CameraCommandSet>();
    }

    private static void AddMqttClient(this IServiceCollection services)
    {
        var mqttFactory = new MqttClientFactory();
        IMqttClient mqttClient = mqttFactory.CreateMqttClient();
        services.AddSingleton(mqttClient);
    }
}
