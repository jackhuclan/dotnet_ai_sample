using Microsoft.Agents.AI;
using OpenAI.Responses;
using System.ClientModel;
using Xunit;

namespace dotnet_ai_agent_sample;

public class Agent_OpenAI_Step01_Running : BaseTest
{
    [Fact]
    public async Task TestMethod1()
    {
        AIAgent agent =
            new ResponsesClient(new ApiKeyCredential(OPENAI_API_KEY), new ResponsesClientOptions
            {
                Endpoint = new Uri("https://api.deepseek.com"),
            })
            .AsAIAgent(model: OPENAI_CHAT_MODEL_NAME, instructions: "You are good at telling jokes.", name: "Joker");

        // Once you have the agent, you can invoke it like any other AIAgent.
        var result = await agent.RunAsync("Tell me a joke about a pirate.");

        Assert.NotEmpty(result.Text);
    }
}
