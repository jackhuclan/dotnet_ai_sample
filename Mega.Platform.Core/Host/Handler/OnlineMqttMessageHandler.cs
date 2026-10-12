using Mega.Platform.Core.Client;
using Mega.Platform.Core.Models;
using Mega.Platform.Core.Utils;
using Microsoft.Extensions.Logging;
using MQTTnet;
using System.Text.Json;

namespace Mega.Platform.Core.Host.Handler;

internal class OnlineMqttMessageHandler : IMqttMessageHandler
{
    private readonly IDeviceManager _deviceManager;
    private readonly ILogger<OnlineMqttMessageHandler> _logger;

    public OnlineMqttMessageHandler(
        IDeviceManager deviceManager,
        ILoggerFactory loggerFactory)
    {
        _deviceManager = deviceManager;
        _logger = loggerFactory.CreateLogger<OnlineMqttMessageHandler>();
    }

    public async Task<object> Handle(MqttApplicationMessageReceivedEventArgs args)
    {
        var onlineRequest = JsonSerializer.Deserialize<OnlineRequest>(args.ApplicationMessage.Payload.ToString());
        if (onlineRequest != null)
        {
            if (onlineRequest.Connected)
            {
                var onlineResponse = RegisterDevice(onlineRequest, args);
                args.ApplicationMessage.PayloadSegment = JsonSerializer.Serialize(onlineResponse).GetBytes();
                return onlineResponse;
            }
            else
            {
                var onlineResponse = UnregisterDevice(onlineRequest);
                args.ApplicationMessage.PayloadSegment = JsonSerializer.Serialize(onlineResponse).GetBytes();
                return onlineResponse;
            }
        }

        return new OnlineResponse();
    }

    private OnlineResponse UnregisterDevice(OnlineRequest onlineRequest)
    {
        _deviceManager.RemoveDevice(onlineRequest.DeviceId);
        return new OnlineResponse
        {
            ProductId = onlineRequest.ProductId,
            DeviceId = onlineRequest.DeviceId,
            Code = ErrorCodes.Sys.SUCCESS
        };
    }

    private OnlineResponse RegisterDevice(OnlineRequest onlineRequest, MqttApplicationMessageReceivedEventArgs arg)
    {
        if (!_deviceManager.TryGetDevice(onlineRequest.DeviceId, out var existingDevice))
        {
            var deviceDescriptor = new DeviceDescriptor
            {
                ProductId = onlineRequest.ProductId,
                DeviceId = onlineRequest.DeviceId,
                DeviceName = onlineRequest.DeviceName,
            };

            _deviceManager.AddDevice(deviceDescriptor);
        }

        return new OnlineResponse
        {
            ProductId = existingDevice.ProductId,
            DeviceId = existingDevice.DeviceId,
            ClientId = arg.ClientId,
            Code = ErrorCodes.Sys.SUCCESS
        };
    }
}
