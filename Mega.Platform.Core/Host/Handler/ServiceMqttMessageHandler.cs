using Microsoft.Extensions.Logging;
using MQTTnet;

namespace Mega.Platform.Core.Host.Handler;

internal class ServiceMqttMessageHandler : IMqttMessageHandler
{
    private readonly ILogger<ServiceMqttMessageHandler> _logger;

    public ServiceMqttMessageHandler(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ServiceMqttMessageHandler>();
    }

    public Task<object> Handle(MqttApplicationMessageReceivedEventArgs applicationMessageArgs) => throw new NotImplementedException();
}
