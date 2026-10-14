using Mega.Platform.Core.Models;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<CommandManager> _logger;
    public CommandManager(IMqttClient mqttClient,
        ILoggerFactory loggerFactory)
    {
        _mqttClient = mqttClient;
        _logger = loggerFactory.CreateLogger<CommandManager>();
    }

    /// <summary>
    /// bind a remote command to a device. if CommandPath exists, the old command with same path will be replaced!
    /// </summary>
    /// <param name="deviceDescriptor"></param>
    /// <param name="remoteCommand"></param>
    public async Task AddCommand(DeviceDescriptor deviceDescriptor, IRemoteCommand remoteCommand)
    {
        _commands[remoteCommand.Descriptor.CommandPath] = remoteCommand;
        await PublishCommand(deviceDescriptor, remoteCommand);
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

    private async Task PublishCommand(DeviceDescriptor deviceDescriptor, IRemoteCommand command)
    {
        try
        {
            var request = new DeviceCommandReportRequest
            {
                ProductId = deviceDescriptor.ProductId,
                DeviceId = deviceDescriptor.DeviceId,
                ClientId = deviceDescriptor.DeviceId,
                CommandId = command.Descriptor.CommandId,
                CommandPath = command.Descriptor.CommandPath,
                Description = command.Descriptor.Description
            };

            byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
            var publishResult = await _mqttClient.PublishBinaryAsync(MqttTopics.GetSubscribeTopic(MqttTopics.CommandTopicTemplate,
                deviceDescriptor.ProductId,
                deviceDescriptor.DeviceId), payload, MqttQualityOfServiceLevel.AtLeastOnce);
            _logger.LogInformation($"{deviceDescriptor.DeviceId} reported, publishResult={publishResult.ReasonCode}");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

}
