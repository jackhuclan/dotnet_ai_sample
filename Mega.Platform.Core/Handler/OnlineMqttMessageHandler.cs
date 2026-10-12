using MQTTnet;
using System.Text.Json;

namespace Mega.Platform.Core.Handler;

internal class OnlineMqttMessageHandler : IMqttMessageHandler
{
    private readonly IDeviceProxyFactory deviceProxyFactory;
    private readonly IDeviceManager deviceHolder;
    private readonly ILogger<OnlineMqttMessageHandler> logger;

    public OnlineMqttMessageHandler(
        IDeviceProxyFactory deviceProxyFactory,
        IDeviceManager deviceHolder,
        ILoggerFactory loggerFactory)
    {
        this.deviceProxyFactory = deviceProxyFactory;
        this.deviceHolder = deviceHolder;
        logger = loggerFactory.CreateLogger<OnlineMqttMessageHandler>();
    }

    public async Task<object> Handle(MqttApplicationMessageReceivedEventArgs args)
    {
        var onlineRequest = JsonSerializer.Deserialize<OnlineRequest>(args.ApplicationMessage.PayloadSegment);
        if (onlineRequest != null)
        {
            if (onlineRequest.Connected)
            {
                var onlineResponse = await RegisterDevice(onlineRequest, args);
                args.ApplicationMessage.PayloadSegment = JsonSerializer.Serialize(onlineResponse).GetBytes();
                return onlineResponse;
            }
            else
            {
                var onlineResponse = await UnregisterDevice(onlineRequest);
                args.ApplicationMessage.PayloadSegment = JsonSerializer.Serialize(onlineResponse).GetBytes();
                return onlineResponse;
            }
        }

        return new OnlineResponse();
    }

    private async Task<OnlineResponse> UnregisterDevice(OnlineRequest onlineRequest)
    {
        await deviceHolder.OfflineDevice(onlineRequest.Descriptor.DeviceId);
        return new OnlineResponse
        {
            ProductId = onlineRequest.Descriptor.ProductId,
            DeviceId = onlineRequest.Descriptor.DeviceId,
            Code = ErrorCodes.Sys.SUCCESS
        };
    }

    private async Task<OnlineResponse> RegisterDevice(OnlineRequest onlineRequest, MqttApplicationMessageReceivedEventArgs arg)
    {
        var deviceProxy = deviceProxyFactory.Create();
        deviceProxy.Descriptor = onlineRequest.Descriptor;
        deviceProxy.ClientId = arg.ClientId;
        deviceProxy.ClientIp = onlineRequest.Descriptor.HostAddress;
        deviceProxy.Status = DeviceStatus.Online;

        await deviceHolder.AddOrUpdateDevice(deviceProxy);

        var response = new OnlineResponse
        {
            ProductId = deviceProxy.Descriptor.ProductId,
            DeviceId = deviceProxy.Descriptor.DeviceId,
            ClientId = arg.ClientId,
            Code = ErrorCodes.Sys.SUCCESS
        };
        return response;
    }
}
