using Mega.Platform.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Mega.Platform.Core.Host;

internal class HostDeviceServiceInvoker : IDeviceServiceInvoker
{
    private readonly IMqttMessagePublisher _mqttMessagePublisher;
    private readonly DeviceCommandRequest _commandRequest;
    private ILogger<HostDeviceServiceInvoker> _logger;

    public HostDeviceServiceInvoker(IMqttMessagePublisher mqttMessagePublisher,
        ILoggerFactory loggerFactory,
        DeviceCommandRequest commandRequest)
    {
        _mqttMessagePublisher = mqttMessagePublisher;
        _commandRequest = commandRequest;
        _logger = loggerFactory.CreateLogger<HostDeviceServiceInvoker>();
    }

    public async Task<DeviceServiceInvokeResponse> InvokeService(DeviceServiceInvokeRequest request)
    {
        Throw.IfNull(request);
        var servicepath = MqttTopics.GetSubscribeTopic(MqttTopics.ServiceTopicTemplate, _commandRequest.ProductId, _commandRequest.DeviceId);
        request.ServiceId = _commandRequest.CommandId;
        request.ServicePath = _commandRequest.CommandPath;
        request.ClientId = _commandRequest.DeviceId;
        request.ProductId = _commandRequest.ProductId;
        request.DeviceId = _commandRequest.DeviceId;
        request.RequestTopic = servicepath;
        request.ReplyTopic = servicepath + "/reply";

        var response = await ExecuteServiceInvocation(request);

        return response;
    }

    private async Task<DeviceServiceInvokeResponse> ExecuteServiceInvocation(DeviceServiceInvokeRequest request)
    {
        try
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(600));
            CancellationToken cancellationToken = tokenSource.Token;
            var requestTopic = request.RequestTopic;
            var replyTopic = request.ReplyTopic;

            var response = await _mqttMessagePublisher.Publish<DeviceServiceInvokeRequest, DeviceServiceInvokeResponse>(requestTopic, replyTopic, request) ?? new DeviceServiceInvokeResponse()
            {
                Code = ErrorCodes.Sys.FAIL,
                Message = "调用服务时返回类型不正确"
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"{request.DeviceId} - {request.ServicePath}-{ex.Message}");
            return new DeviceServiceInvokeResponse
            {
                Code = ErrorCodes.Sys.FAIL,
                Message = ex.Message
            };
        }
    }
}
