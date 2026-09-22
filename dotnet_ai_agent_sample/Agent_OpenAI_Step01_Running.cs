using Microsoft.Agents.AI;
using OpenAI.Responses;
using System.ClientModel;

namespace dotnet_ai_agent_sample;

public class Agent_OpenAI_Step01_Running : BaseTest
{
    public async Task Run()
    {
        AIAgent agent =
            new ResponsesClient(new ApiKeyCredential(OPENAI_API_KEY), new ResponsesClientOptions
            {
                Endpoint = new Uri("https://api.deepseek.com"),
            })
            .AsAIAgent(model: OPENAI_CHAT_MODEL_NAME, instructions: "You are good at telling jokes.", name: "Joker");

        // Once you have the agent, you can invoke it like any other AIAgent.
        Console.WriteLine(await agent.RunAsync("给我讲一个关于海盗的笑话。"));
    }
}
