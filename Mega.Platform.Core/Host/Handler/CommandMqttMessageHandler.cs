using Mega.Platform.Core.Host.Agent;
using Mega.Platform.Core.Models;
using Mega.Platform.Core.Utils;
using Microsoft.Extensions.Logging;
using MQTTnet;
using System.Text.Json;

namespace Mega.Platform.Core.Host.Handler;

internal class CommandMqttMessageHandler : IMqttMessageHandler
{
    private readonly IAIToolManager _aIToolManager;
    private readonly ILogger<CommandMqttMessageHandler> _logger;
    public CommandMqttMessageHandler(IAIToolManager aIToolManager,
        ILoggerFactory loggerFactory)
    {
        _aIToolManager = aIToolManager;
        _logger = loggerFactory.CreateLogger<CommandMqttMessageHandler>();
    }

    public Task<object> Handle(MqttApplicationMessageReceivedEventArgs args)
    {
        var commandRequest = JsonSerializer.Deserialize<DeviceCommandRequest>(args.ApplicationMessage.Payload.ToStr());
        if (commandRequest == null)
        {
            _logger.LogError("Command request is null");
            return Task.FromResult<object>(new DeviceCommandResponse
            {
                Code = ErrorCodes.Sys.FAIL,
                Message = "Command request is null",
            });
        }

        _aIToolManager.RegisterTool(commandRequest);
        var response = new DeviceCommandResponse
        {
            Code = ErrorCodes.Sys.SUCCESS,
            Message = "",
        };

        return Task.FromResult<object>(response);
    }
}
