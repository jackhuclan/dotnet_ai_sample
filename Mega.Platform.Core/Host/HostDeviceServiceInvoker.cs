using Mega.Platform.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Mega.Platform.Core.Host;

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
        Throw.IfNullOrEmpty(request.ServicePath);
        Throw.IfNullOrEmpty(request.ProductId);
        Throw.IfNullOrEmpty(request.DeviceId);

        var response = await ExecuteServiceInvocation(request);

        return response;
    }

    private async Task<DeviceServiceInvokeResponse> ExecuteServiceInvocation(DeviceServiceInvokeRequest request)
    {
        try
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(500));
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
