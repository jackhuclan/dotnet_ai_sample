using Microsoft.Extensions.Logging;
using MQTTnet.Protocol;
using System.Text;
using System.Text.Json;

namespace Mega.Platform.Core.Client;

public class DefaultClientMqttListener : IClientMqttListener
{
    private readonly IClientMqttApplicationMessageListener _applicationMessageListner;
    private readonly IMqttClient _mqttClient;
    private readonly ILogger<DefaultClientMqttListener> _logger;

    public DefaultClientMqttListener(IClientMqttApplicationMessageListener applicationMessageListner,
        IMqttClient mqttClient,
        ILoggerFactory loggerFactory)
    {
        _applicationMessageListner = applicationMessageListner;
        _mqttClient = mqttClient;
        _logger = loggerFactory.CreateLogger<DefaultClientMqttListener>();
    }

    public virtual async Task OnConnected(Device device)
    {
        _logger.LogInformation($"OnMqttConnected {device.DeviceId} MqttConnected=true");

        var reported = await OnHeartbeat(device);

        if (!reported) return;

        foreach (var command in device.Commands.Where(x => (x.Value.Descriptor.UsageScope & CommandUsageKind.Mqtt) == CommandUsageKind.Mqtt))
        {
            await SubscribeService(command.Value);
        }
    }

    public virtual Task OnDisonnected(Device device)
    {
        _applicationMessageListner.Services.Clear();
        _logger.LogInformation($"Mqtt disconnected on {device.DeviceId}!");
        return Task.CompletedTask;
    }

    public virtual async Task<bool> OnHeartbeat(Device device)
    {
        try
        {
            var request = new OnlineRequest
            {
                Descriptor = device.DeviceDescriptor,
                Connected = true,
            };

            byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
            var publishResult = await _mqttClient.PublishBinaryAsync(Topics.Upstream.OnlineTopic, payload, MqttQualityOfServiceLevel.ExactlyOnce);
            _logger.LogInformation($"TryReportMqttCentralOnline {device.DeviceDescriptor.DeviceId} when MqttConnected=true,publishResult={publishResult.ReasonCode}");
            return publishResult != null && publishResult.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }

    public async Task SubscribeService(string commandPath,
        CommandFunction commandFunction,
        MqttQualityOfServiceLevel mqttQualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce)
    {
        try
        {
            if (_applicationMessageListner.Services.TryAdd(commandPath, commandFunction))
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

    public async Task SubscribeService(IRemoteCommand remoteCommand, MqttQualityOfServiceLevel mqttQualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce)
    {
        try
        {
            if (_applicationMessageListner.Services.TryAdd(remoteCommand.Descriptor.CommandPath, remoteCommand.Invoke))
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
}
