using Mega.Platform.Core;
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
[Route("v1/agent")]
public class AgentController : ControllerBase
{
    private readonly IAgentManager _agentManager;
    private readonly ISessionManager _sessionManager;
    private readonly CameraCommandSet _cameraCommandSet;

    public AgentController(IAgentManager agentManager,
        ISessionManager sessionManager,
        CameraCommandSet cameraCommandSet)
    {
        _agentManager = agentManager;
        _sessionManager = sessionManager;
        _cameraCommandSet = cameraCommandSet;
    }

    [HttpPost("/chat")]
    public async Task<ChatResponse> Chat([FromBody] ChatRequest chatRequest, CancellationToken cancellationToken)
    {
        var mainAgentName = $"{chatRequest.ProductId}_{chatRequest.DeviceId}_main_agent";
        var agent = _agentManager.GetOrCreateAgent(mainAgentName, CreateAgent);
        //if (!_sessionManager.TryGetSession(chatRequest.SessionId, out DeviceAgentSession session))
        //{
        //    session = (DeviceAgentSession)await agent.CreateSessionAsync(cancellationToken);
        //}
        var servicepath = MqttTopics.GetSubscribeTopic(MqttTopics.ServiceTopicTemplate, chatRequest.ProductId, chatRequest.DeviceId);
        var json = JsonSerializer.Serialize(new DeviceServiceInvokeRequest
        {
            ServiceId = "camera",
            ServicePath = "aoi/form1/button1_Click",
            ProductId = chatRequest.ProductId,
            DeviceId = chatRequest.DeviceId,
            RequestTopic = servicepath,
            ReplyTopic = servicepath + "/reply",
        });
        var prompt = $"请打开设备{chatRequest.DeviceId}，执行参数deviceServiceInvokeRequest如下：" + Environment.NewLine
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
                instructions: "You are a helpful assistant that can control devices and provide information about them.",
                tools: _cameraCommandSet.AsTools()
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
