using Mega.Platform.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using System.Text;
using System.Text.Json;

namespace Mega.Platform.Core.Client.Command;

/// <summary>
/// 指令集管理
/// </summary>
public class CommandManager : ICommandManager
{
    private readonly Dictionary<string, IRemoteCommand> _commands = new();
    private readonly IMqttClient _mqttClient;
    private readonly IMqttClientWrapper _mqttClientWrapper;
    private readonly ILogger<CommandManager> _logger;
    private readonly DeviceDescriptor _deviceDescriptor;
    public CommandManager(IOptions<DeviceDescriptor> deviceDescriptorOptions,
        IMqttClient mqttClient,
        IMqttClientWrapper mqttClientWrapper,
        ILoggerFactory loggerFactory)
    {
        _deviceDescriptor = deviceDescriptorOptions.Value;
        _mqttClient = mqttClient;
        _mqttClientWrapper = mqttClientWrapper;
        _mqttClientWrapper.OnConnected += OnMqttClientConnected;
        _logger = loggerFactory.CreateLogger<CommandManager>();
    }
    /// <summary>
    /// bind a remote command to a device. if CommandPath exists, the old command with same path will be replaced!
    /// </summary>
    /// <param name="deviceDescriptor"></param>
    /// <param name="remoteCommand"></param>
    public async Task AddCommand(IRemoteCommand remoteCommand)
    {
        _commands[remoteCommand.Descriptor.CommandPath] = remoteCommand;
    }

    public bool TryGetCommand(string commandPath, out IRemoteCommand? command)
    {
        if (!string.IsNullOrWhiteSpace(commandPath)
            && _commands.ContainsKey(commandPath))
        {
            command = _commands[commandPath];
            return true;
        }

        command = null;
        return false;
    }

    private async Task OnMqttClientConnected()
    {
        foreach (var item in _commands)
        {
            await PublishCommand(item.Value);
        }
    }

    private async Task PublishCommand(IRemoteCommand command)
    {
        try
        {
            var request = new DeviceCommandRequest
            {
                ProductId = _deviceDescriptor.ProductId,
                DeviceId = _deviceDescriptor.DeviceId,
                ClientId = _deviceDescriptor.DeviceId,
                CommandId = command.Descriptor.CommandId,
                CommandPath = command.Descriptor.CommandPath,
                Description = command.Descriptor.Description
            };

            byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
            var publishResult = await _mqttClient.PublishBinaryAsync(MqttTopics.GetSubscribeTopic(MqttTopics.CommandTopicTemplate,
                _deviceDescriptor.ProductId,
                _deviceDescriptor.DeviceId), payload, MqttQualityOfServiceLevel.AtLeastOnce);
            _logger.LogInformation($"{_deviceDescriptor.DeviceId}'s command {command.Descriptor.CommandId} reported, publishResult={publishResult.ReasonCode}");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

}
