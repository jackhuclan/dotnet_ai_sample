using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace dotnet_ai_agent_sample;

internal class Agent_OpenAI_Step03_CreateFromChatClient : BaseTest
{
    public async Task Run()
    {
        // Create a ChatClient directly from OpenAIClient
        ChatClient chatClient = new OpenAIClient(new ApiKeyCredential(OPENAI_API_KEY),
            new OpenAIClientOptions
            {
                Endpoint = new Uri("https://api.deepseek.com"),
            }).GetChatClient(OPENAI_CHAT_MODEL_NAME);

        // Create an agent directly from the ChatClient using OpenAIChatClientAgent
        OpenAIChatClientAgent agent = new(chatClient, instructions: "You are good at telling jokes.", name: "Joker");

        UserChatMessage chatMessage = new("给我讲一个关于海盗的笑话。");

        // Invoke the agent and output the text result.
        ChatCompletion chatCompletion = await agent.RunAsync([chatMessage]);
        Console.WriteLine(chatCompletion.Content.Last().Text);

        // Invoke the agent with streaming support.
        IAsyncEnumerable<StreamingChatCompletionUpdate> completionUpdates = agent.RunStreamingAsync([chatMessage]);
        await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
        {
            if (completionUpdate.ContentUpdate.Count > 0)
            {
                Console.Write(completionUpdate.ContentUpdate[0].Text);
            }
        }
    }
}
