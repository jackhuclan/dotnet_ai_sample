using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Diagnostics.Logger;

namespace Mega.Platform.Core.Client;

public static class MqttClientBuilder
{
    public static void AddMqttClient(this IServiceCollection services, IConfiguration configuration)
    {
        var logger = new MqttNetEventLogger();
        var mqttFactory = new MqttClientFactory(logger);
        IMqttClient mqttClient = mqttFactory.CreateMqttClient();
        services.AddSingleton(mqttClient);
        services.AddSingleton(logger);
        services.AddSingleton<IMqttClientLogger, MqttClientLogger>();
    }
}
