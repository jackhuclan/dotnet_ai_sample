using Microsoft.Extensions.Logging;
using MQTTnet;

namespace Mega.Platform.Core.Host.Handler;

internal class ServiceMqttMessageHandler : IMqttMessageHandler
{
    private readonly ILogger<ServiceMqttMessageHandler> _logger;
    private readonly IDeviceServiceInvoker _deviceServiceInvoker;

    public ServiceMqttMessageHandler(IDeviceServiceInvoker deviceServiceInvoker,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ServiceMqttMessageHandler>();
        _deviceServiceInvoker = deviceServiceInvoker;
    }

    public Task<object> Handle(MqttApplicationMessageReceivedEventArgs applicationMessageArgs) => throw new NotImplementedException();
}
