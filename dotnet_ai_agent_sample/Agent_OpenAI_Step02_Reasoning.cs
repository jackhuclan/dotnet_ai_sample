using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace dotnet_ai_agent_sample;

public class Agent_OpenAI_Step02_Reasoning : BaseTest
{
    public async Task Run()
    {
        var client = new OpenAIClient(new ApiKeyCredential(OPENAI_API_KEY), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com"),
        }).GetResponsesClient()
         .AsIChatClient(OPENAI_CHAT_MODEL_NAME)
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

        AIAgent agent = new ChatClientAgent(client);

        Console.WriteLine("1. Non-streaming:");
        var response = await agent.RunAsync("解决下面的问题：如果一辆火车以每小时60英里的速度行驶，需要行驶180英里，那么行程需要多长时间？请展示你的推理过程。");

        Console.WriteLine(response.Text);

        Console.WriteLine("Token usage:");
        Console.WriteLine($"Input: {response.Usage?.InputTokenCount}, Output: {response.Usage?.OutputTokenCount}, {string.Join(", ", response.Usage?.AdditionalCounts ?? [])}");
        //Console.WriteLine();

        Console.WriteLine("2. Streaming");
        await foreach (var update in agent.RunStreamingAsync("用简单的话解释相对论。"))
        {
            foreach (var item in update.Contents)
            {
                if (item is TextReasoningContent reasoningContent)
                {
                    Console.Write($"\e[97m{reasoningContent.Text}\e[0m");
                }
                else if (item is TextContent textContent)
                {
                    Console.Write(textContent.Text);
                }
            }
        }
    }
}
