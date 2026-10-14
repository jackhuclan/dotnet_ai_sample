using Mega.Platform.Core;
using Mega.Platform.Core.Host;
using Mega.Platform.Core.Host.Agent;
using Mega.Platform.Core.Host.Command;
using Mega.Platform.Core.Models;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;
using System.Text.Json;

namespace Mega.Platform.Web.Controllers;

[ApiController]
public class AgentController : ControllerBase
{
    private readonly IAgentManager _agentManager;
    private readonly ISessionManager _sessionManager;
    private readonly IDeviceManager _deviceManager;
    private readonly IAIToolManager _aIToolManager;
    private readonly CameraCommandSet _cameraCommandSet;

    public AgentController(IAgentManager agentManager,
        ISessionManager sessionManager,
        IDeviceManager deviceManager,
        IAIToolManager aIToolManager,
        CameraCommandSet cameraCommandSet)
    {
        _agentManager = agentManager;
        _sessionManager = sessionManager;
        _deviceManager = deviceManager;
        _aIToolManager = aIToolManager;
        _cameraCommandSet = cameraCommandSet;
    }

    [HttpGet("/devices")]
    public async Task<IReadOnlyList<string>> GetDevices() => _deviceManager.DeviceIds;

    [HttpPost("/chat")]
    public async Task<ChatResponse> Chat([FromBody] ChatRequest chatRequest, CancellationToken cancellationToken)
    {
        if (!_deviceManager.TryGetDevice(chatRequest.DeviceId, out var deviceDescriptor))
        {
            return new ChatResponse
            {
                Event = "error",
                Data = $"Device with ID {chatRequest.DeviceId} not found."
            };
        }

        var mainAgentName = $"{deviceDescriptor.ProductId}_{chatRequest.DeviceId}_main_agent";
        var agent = _agentManager.GetOrCreateAgent(mainAgentName, CreateAgent);
        //if (!_sessionManager.TryGetSession(chatRequest.SessionId, out DeviceAgentSession session))
        //{
        //    session = (DeviceAgentSession)await agent.CreateSessionAsync(cancellationToken);
        //}
        var servicepath = MqttTopics.GetSubscribeTopic(MqttTopics.ServiceTopicTemplate, deviceDescriptor.ProductId, chatRequest.DeviceId);
        var json = JsonSerializer.Serialize(new DeviceServiceInvokeRequest
        {
            ProductId = deviceDescriptor.ProductId,
            DeviceId = chatRequest.DeviceId,
            RequestTopic = servicepath,
            ReplyTopic = servicepath + "/reply",
        });

        var prompt = $"{chatRequest.Message}，执行参数deviceServiceInvokeRequest如下：" + Environment.NewLine
            + json;

        var response = await agent.RunAsync(prompt, null, null, cancellationToken);
        return new ChatResponse
        {
            Event = "chat_response",
            Data = response.Text,
        };

        AIAgent CreateAgent()
        {
            var envSetting = new EnvSetting();
            var chatClientAgent = new OpenAIClient(new ApiKeyCredential(envSetting.OPENAI_API_KEY), new OpenAIClientOptions
            {
                Endpoint = new Uri("https://api.deepseek.com"),
            }).GetResponsesClient()
             .AsAIAgent(
                name: mainAgentName,
                model: chatRequest.Model,
                instructions: "You are a helpful assistant that can control devices and provide information about them." + Environment.NewLine
                              + "##注意事项：" + Environment.NewLine
                              + "1. 调用工具时，从对应工具的描述中提取其servicePath" + Environment.NewLine
                              + "2. deviceServiceInvokeRequest参数中的serviceId值就是工具的name，servicePath就是工具的servicePath" + Environment.NewLine
                              + "3. 传入deviceServiceInvokeRequest参数时，需要确保参数的完整性和正确性" + Environment.NewLine
                              + "##约束" + Environment.NewLine
                              + "- deviceServiceInvokeRequest参数不完整的时候，不允许调用工具，并给出相应的提示，告知缺少的参数",
                tools: _aIToolManager.Tools
             );

            //var agent = new DeviceChatAgent(chatClientAgent, new DeviceDescriptor
            //{
            //    ProductId = chatRequest.ProductId,
            //    DeviceId = chatRequest.DeviceId,
            //});

            return chatClientAgent;
        }

    }
}
