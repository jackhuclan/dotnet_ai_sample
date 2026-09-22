using Microsoft.Agents.AI;
using OpenAI.Responses;
using System.ClientModel;

namespace dotnet_ai_agent_sample;

public class Agent_OpenAI_Step01_Running
{
    public async Task Run()
    {
        var envSetting = new EnvSetting();
        AIAgent agent =
            new ResponsesClient(new ApiKeyCredential(envSetting.OPENAI_API_KEY), new ResponsesClientOptions
            {
                Endpoint = new Uri("https://api.deepseek.com"),
            })
            .AsAIAgent(model: envSetting.OPENAI_CHAT_MODEL_NAME, instructions: "You are good at telling jokes.", name: "Joker");

        // Once you have the agent, you can invoke it like any other AIAgent.
        Console.WriteLine(await agent.RunAsync("给我讲一个关于海盗的笑话。"));
    }
}
