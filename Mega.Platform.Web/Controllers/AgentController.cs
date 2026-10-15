using Mega.Platform.Core.Host;
using Mega.Platform.Core.Host.Agent;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;

namespace Mega.Platform.Web.Controllers;

[ApiController]
public class AgentController : ControllerBase
{
    private readonly IAgentManager _agentManager;
    private readonly ISessionManager _sessionManager;
    private readonly IDeviceManager _deviceManager;
    private readonly IAIToolManager _aIToolManager;

    public AgentController(IAgentManager agentManager,
        ISessionManager sessionManager,
        IDeviceManager deviceManager,
        IAIToolManager aIToolManager)
    {
        _agentManager = agentManager;
        _sessionManager = sessionManager;
        _deviceManager = deviceManager;
        _aIToolManager = aIToolManager;
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
        var prompt = $"{chatRequest.Message}";

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
                instructions: "You are a helpful assistant that can control devices and provide information about them.",
                //+ Environment.NewLine
                //+ "##约束" + Environment.NewLine
                //+ "- deviceServiceInvokeRequest参数不完整的时候，不允许调用工具，并给出相应的提示，告知缺少的参数",
                tools: _aIToolManager.Tools(chatRequest.DeviceId)
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
