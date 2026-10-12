using Mega.Platform.Core.Client;
using Mega.Platform.Core.Host.Agent;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace Mega.Platform.Web.Controllers;

[ApiController]
[Route("v1/chat")]
public class ChatController : ControllerBase
{
    private readonly IAgentManager _agentManager;
    private readonly ISessionManager _sessionManager;
    private readonly CancellationToken _token = new CancellationTokenSource().Token;

    public ChatController(IAgentManager agentManager,
        ISessionManager sessionManager)
    {
        _agentManager = agentManager;
        _sessionManager = sessionManager;
    }

    public async Task<ChatResponse> Chat(ChatRequest chatRequest)
    {
        var agent = _agentManager.GetOrCreateAgent($"{chatRequest.ProductId}_{chatRequest.DeviceId}_main_agent", CreateAgent);
        if (!_sessionManager.TryGetSession(chatRequest.SessionId, out DeviceAgentSession session))
        {
            session = (DeviceAgentSession)await agent.CreateSessionAsync(_token);
        }

        AgentResponse response = await agent.RunAsync("xxx", session, null, _token);
        return new ChatResponse
        {
            Event = "chat_response",
            Data = response.Text,
        };

        DeviceChatAgent CreateAgent()
        {
            var envSetting = new EnvSetting();
            var client = new OpenAIClient(new ApiKeyCredential(envSetting.OPENAI_API_KEY), new OpenAIClientOptions
            {
                Endpoint = new Uri("https://api.deepseek.com"),
            }).GetResponsesClient()
             .AsIChatClient(envSetting.OPENAI_CHAT_MODEL_NAME)
             .AsBuilder()
             .ConfigureOptions(o =>
             {
                 o.Reasoning = new()
                 {
                     Effort = ReasoningEffort.Medium,
                     Output = ReasoningOutput.Full,
                 };
             })
             .Build();

            var agent = new DeviceChatAgent(client.AsAIAgent(), new DeviceDescriptor
            {
                ProductId = chatRequest.ProductId,
                DeviceId = chatRequest.DeviceId,
            });

            return agent;
        }

    }
}
