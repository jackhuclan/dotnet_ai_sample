using Mega.Platform.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Mega.Platform.Core;

internal class HostDeviceServiceInvoker : IDeviceServiceInvoker
{
    private readonly IMqttMessagePublisher _mqttMessagePublisher;
    private ILogger<HostDeviceServiceInvoker> _logger;

    public HostDeviceServiceInvoker(IMqttMessagePublisher mqttMessagePublisher,
        ILoggerFactory loggerFactory)
    {
        _mqttMessagePublisher = mqttMessagePublisher;
        _logger = loggerFactory.CreateLogger<HostDeviceServiceInvoker>();
    }

    public async Task<DeviceServiceInvokeResponse> InvokeService(DeviceServiceInvokeRequest request, int retryCount = 10)
    {
        Throw.IfNull(request);
        Throw.IfNullOrEmpty(request.ServiceId);
        Throw.IfNullOrEmpty(request.TargetProductId);
        Throw.IfNullOrEmpty(request.TargetDeviceId);
        Throw.IfNullOrEmpty(request.TargetClientId);

        var response = await ExecuteServiceInvocation(request);

        return response;
    }

    private async Task<DeviceServiceInvokeResponse> ExecuteServiceInvocation(DeviceServiceInvokeRequest request)
    {
        var response = new DeviceServiceInvokeResponse();

        try
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(5));
            CancellationToken cancellationToken = tokenSource.Token;
            var requestTopic = request.RequestTopic;
            var replyTopic = request.ReplyTopic;
            var invokeResult = await _mqttMessagePublisher.Publish<DeviceServiceInvokeRequest, DeviceServiceInvokeResponse>(requestTopic, replyTopic, request);
            if (invokeResult == null)
            {
                response.Code = ErrorCodes.Sys.FAIL;
                response.Message = "调用服务时返回类型不正确";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"{request.DeviceId} - {request.TargetDeviceId}-{ex.Message}");
            response = new DeviceServiceInvokeResponse
            {
                Code = ErrorCodes.Sys.FAIL,
                Message = ex.Message
            };
        }

        return response ?? new DeviceServiceInvokeResponse()
        {
            Code = ErrorCodes.Sys.FAIL,
            Message = ErrorCodes.Sys.UNKNOWN_MESSAGE
        };
    }
}
