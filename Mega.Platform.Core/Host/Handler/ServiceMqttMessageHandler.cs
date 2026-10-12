using Mega.Platform.Core.Host;
using System.Text.Json;

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

    public async Task<object> Handle(MqttApplicationMessageReceivedEventArgs args)
    {
        var request = JsonSerializer.Deserialize<DeviceServiceInvokeRequest>(args.ApplicationMessage.PayloadSegment);
        ThrowHelper.ThrowArgumentNullException(request);
        ThrowHelper.ThrowArgumentNullOrWhiteSpaceException(request.ProductId);
        ThrowHelper.ThrowArgumentNullOrWhiteSpaceException(request.DeviceId);
        ThrowHelper.ThrowArgumentNullOrWhiteSpaceException(request.ServiceId);
        ThrowHelper.ThrowArgumentNullOrWhiteSpaceException(request.TargetProductId);
        ThrowHelper.ThrowArgumentNullOrWhiteSpaceException(request.TargetDeviceId);

        var response = await _deviceServiceInvoker.InvokeService(request);
        return response;
    }
}
