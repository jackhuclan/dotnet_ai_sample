using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Mega.Platform.Core.Client;

public class DefaultClientMqttApplicationMessageListener : IClientMqttApplicationMessageListener
{
    private readonly ILogger<DefaultClientMqttApplicationMessageListener> _logger;
    private readonly IMqttClient _mqttClient;
    private readonly IDeviceProvider _deviceProvider;
    private readonly ConcurrentDictionary<string, CommandFunction> _services;

    public DefaultClientMqttApplicationMessageListener(IMqttClient mqttClient,
        IDeviceProvider deviceProvider,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DefaultClientMqttApplicationMessageListener>();
        _services = new ConcurrentDictionary<string, CommandFunction>();
        _mqttClient = mqttClient;
        _deviceProvider = deviceProvider;
    }

    public ConcurrentDictionary<string, CommandFunction> Services => _services;

    public virtual async Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        if (arg.ApplicationMessage.Topic == Topics.Upstream.OnlineTopic
            && arg.ReasonCode == MqttApplicationMessageReceivedReasonCode.Success)
        {
            await Task.Run(() =>
            {
                var onlineResponse = JsonSerializer.Deserialize<OnlineResponse>(arg.ApplicationMessage.PayloadSegment);
                if (onlineResponse == null) return;
                var device = _deviceProvider.Devices.FirstOrDefault(d => d.DeviceId == onlineResponse.DeviceId);
                if (device == null) return;
                device.ClientId = arg.ClientId;
                _logger.LogInformation($"Retrieved {device.DeviceId}'s clientId={arg.ClientId}");
            });
        }

        if (_services.ContainsKey(arg.ApplicationMessage.Topic))
        {
            var deviceServiceInvokeRequest = JsonSerializer.Deserialize<DeviceServiceInvokeRequest>(arg.ApplicationMessage.PayloadSegment);
            if (deviceServiceInvokeRequest == null) return;
            _logger.LogDebug($"MqttClient_ApplicationMessageReceivedAsync-1:{arg.ApplicationMessage.Topic}-{deviceServiceInvokeRequest.DeviceId} -> {deviceServiceInvokeRequest.TargetDeviceId} - {deviceServiceInvokeRequest.ServiceId}");
            var response = await _services[arg.ApplicationMessage.Topic].Invoke(deviceServiceInvokeRequest);
            _logger.LogDebug($"MqttClient_ApplicationMessageReceivedAsync-2:{arg.ApplicationMessage.Topic}-{deviceServiceInvokeRequest.DeviceId} -> {deviceServiceInvokeRequest.TargetDeviceId} - {deviceServiceInvokeRequest.ServiceId}");

            //mqtt 5.0
            if (!string.IsNullOrWhiteSpace(arg.ApplicationMessage.ResponseTopic))
            {
                await _mqttClient.PublishBinaryAsync(arg.ApplicationMessage.ResponseTopic, JsonSerializer.Serialize(response).GetBytes(), MqttQualityOfServiceLevel.ExactlyOnce);
                _logger.LogDebug($"MqttClient_ApplicationMessageReceivedAsync-3:{arg.ApplicationMessage.ResponseTopic}-{deviceServiceInvokeRequest.DeviceId} -> {deviceServiceInvokeRequest.TargetDeviceId} - {deviceServiceInvokeRequest.ServiceId}");
            }
            //reply service response to caller
            else if (!string.IsNullOrWhiteSpace(deviceServiceInvokeRequest.ReplyTopic) && response != null)
            {
                await _mqttClient.PublishBinaryAsync(deviceServiceInvokeRequest.ReplyTopic, JsonSerializer.Serialize(response).GetBytes(), MqttQualityOfServiceLevel.ExactlyOnce);
                _logger.LogDebug($"MqttClient_ApplicationMessageReceivedAsync-3:{deviceServiceInvokeRequest.ReplyTopic}-{deviceServiceInvokeRequest.DeviceId} -> {deviceServiceInvokeRequest.TargetDeviceId} - {deviceServiceInvokeRequest.ServiceId}");
            }
        }
    }
}
