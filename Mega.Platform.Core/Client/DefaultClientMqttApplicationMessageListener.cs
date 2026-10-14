using Mega.Platform.Core.Client.Command;
using Mega.Platform.Core.Models;
using Mega.Platform.Core.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;
using MQTTnet;
using MQTTnet.Protocol;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace Mega.Platform.Core.Client;

public class DefaultClientMqttApplicationMessageListener : IClientMqttApplicationMessageListener
{
    private readonly ILogger<DefaultClientMqttApplicationMessageListener> _logger;
    private readonly IMqttClient _mqttClient;
    public ConcurrentDictionary<string, CommandFunction> Services => _services;
    private readonly ConcurrentDictionary<string, CommandFunction> _services;
    private IDevice? _device;
    public DefaultClientMqttApplicationMessageListener(IMqttClient mqttClient,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DefaultClientMqttApplicationMessageListener>();
        _services = new ConcurrentDictionary<string, CommandFunction>();
        _mqttClient = mqttClient;
    }

    public virtual async Task OnConnected(IDevice device)
    {
        _device = device;
        _logger.LogInformation($"OnMqttConnected {device.DeviceId} MqttConnected=true");

        await Report(device, true);
    }

    public virtual async Task OnDisconnected(IDevice device)
    {
        _services.Clear();
        await Report(device, false);
        _logger.LogInformation($"Mqtt disconnected on {device.DeviceId}!");
    }

    public virtual async Task<bool> OnHeartbeat(IDevice device)
    {
        return await Report(device, true);
    }

    private async Task<bool> Report(IDevice device, bool reported)
    {
        try
        {
            var request = new OnlineRequest
            {
                ProductId = device.ProductId,
                DeviceId = device.DeviceId,
                DeviceName = device.DeviceName,
                Connected = reported,
            };

            byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
            var publishResult = await _mqttClient.PublishBinaryAsync(MqttTopics.GetSubscribeTopic(MqttTopics.OnlineTopicTemplate,
                device.ProductId,
                device.DeviceId), payload, MqttQualityOfServiceLevel.AtLeastOnce);
            var msg = reported ? "login" : "logout";
            _logger.LogInformation($"{device.DeviceId} report {msg}, publishResult={publishResult.ReasonCode}");

            return publishResult != null && publishResult.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }

    public async Task SubscribeService(IDevice device,
        string commandPath,
        CommandFunction commandFunction,
        MqttQualityOfServiceLevel mqttQualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce)
    {
        try
        {
            if (_services.TryAdd(commandPath, commandFunction))
            {
                var subscribeResult = await _mqttClient.SubscribeAsync(commandPath, mqttQualityOfServiceLevel);
                _logger.LogInformation($"SubscribeService: {commandPath},ResultCode={subscribeResult.Items.FirstOrDefault()?.ResultCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    public async Task SubscribeService(IDevice device,
        IRemoteCommand remoteCommand,
        MqttQualityOfServiceLevel mqttQualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce)
    {
        try
        {
            if (_services.TryAdd(remoteCommand.Descriptor.CommandPath, remoteCommand.Invoke))
            {
                var subscribeResult = await _mqttClient.SubscribeAsync(remoteCommand.Descriptor.CommandPath, mqttQualityOfServiceLevel);
                _logger.LogInformation($"SubscribeService: {remoteCommand.Descriptor.CommandPath},ResultCode={subscribeResult.Items.FirstOrDefault()?.ResultCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    public virtual async Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        Throw.IfNull(_device);

        var msg = arg.ApplicationMessage.Payload.ToStr();
        //if (arg.ApplicationMessage.Topic == MqttTopics.OnlineTopicTemplate
        //    && arg.ReasonCode == MqttApplicationMessageReceivedReasonCode.Success)
        //{
        //    await Task.Run(() =>
        //    {
        //        var onlineResponse = JsonSerializer.Deserialize<OnlineResponse>(msg);
        //        if (onlineResponse == null) return;
        //        _device.ClientId = arg.ClientId;
        //        _logger.LogInformation($"Retrieved {_device.DeviceId}'s clientId={arg.ClientId}");
        //    });
        //}

        var deviceServiceInvokeRequest = JsonSerializer.Deserialize<DeviceServiceInvokeRequest>(msg);
        if (deviceServiceInvokeRequest == null) return;

        if (_services.ContainsKey(deviceServiceInvokeRequest.ServicePath))
        {
            _logger.LogDebug($"MqttClient_ApplicationMessageReceivedAsync-1:{arg.ApplicationMessage.Topic}-{deviceServiceInvokeRequest.DeviceId} -> {deviceServiceInvokeRequest.ServicePath}");
            var response = await _services[deviceServiceInvokeRequest.ServicePath].Invoke(deviceServiceInvokeRequest);
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
