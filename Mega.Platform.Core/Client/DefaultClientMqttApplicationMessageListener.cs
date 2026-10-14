using Mega.Platform.Core.Client.Command;
using Mega.Platform.Core.Models;
using Mega.Platform.Core.Utils;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using System.Text;
using System.Text.Json;

namespace Mega.Platform.Core.Client;

public class DefaultClientMqttApplicationMessageListener : IClientMqttApplicationMessageListener
{
    private readonly ILogger<DefaultClientMqttApplicationMessageListener> _logger;
    private readonly IMqttClient _mqttClient;
    private readonly ICommandManager _commandManager;

    public DefaultClientMqttApplicationMessageListener(IMqttClient mqttClient,
        ICommandManager commandManager,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DefaultClientMqttApplicationMessageListener>();
        _mqttClient = mqttClient;
        _commandManager = commandManager;
    }

    public virtual async Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        var msg = arg.ApplicationMessage.Payload.ToStr();
        var deviceServiceInvokeRequest = JsonSerializer.Deserialize<DeviceServiceInvokeRequest>(msg);
        if (deviceServiceInvokeRequest == null) return;

        if (_commandManager.TryGetCommand(deviceServiceInvokeRequest.ServicePath, out var commandFunction)
            && commandFunction != null)
        {
            _logger.LogDebug($"MqttClient_ApplicationMessageReceivedAsync-1:{arg.ApplicationMessage.Topic}-{deviceServiceInvokeRequest.DeviceId} -> {deviceServiceInvokeRequest.ServicePath}");
            var response = await commandFunction.Invoke(deviceServiceInvokeRequest);
            _logger.LogDebug($"MqttClient_ApplicationMessageReceivedAsync-2:{arg.ApplicationMessage.Topic}-{deviceServiceInvokeRequest.DeviceId} -> {deviceServiceInvokeRequest.ServicePath}");

            //mqtt 5.0
            if (!string.IsNullOrWhiteSpace(arg.ApplicationMessage.ResponseTopic))
            {
                await _mqttClient.PublishBinaryAsync(arg.ApplicationMessage.ResponseTopic, JsonSerializer.Serialize(response).GetBytes(), MqttQualityOfServiceLevel.ExactlyOnce);
                _logger.LogDebug($"MqttClient_ApplicationMessageReceivedAsync-3:{arg.ApplicationMessage.ResponseTopic}-{deviceServiceInvokeRequest.DeviceId} -> {deviceServiceInvokeRequest.ServicePath}");
            }
            //reply service response to caller
            else if (!string.IsNullOrWhiteSpace(deviceServiceInvokeRequest.ReplyTopic) && response != null)
            {
                await _mqttClient.PublishBinaryAsync(deviceServiceInvokeRequest.ReplyTopic, JsonSerializer.Serialize(response).GetBytes(), MqttQualityOfServiceLevel.ExactlyOnce);
                _logger.LogDebug($"MqttClient_ApplicationMessageReceivedAsync-3:{deviceServiceInvokeRequest.ReplyTopic}-{deviceServiceInvokeRequest.DeviceId} -> {deviceServiceInvokeRequest.ServicePath}");
            }
        }
    }
}
