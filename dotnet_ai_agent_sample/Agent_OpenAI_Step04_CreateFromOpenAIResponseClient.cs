using OpenAI;
using OpenAI.Responses;
using System.ClientModel;

namespace dotnet_ai_agent_sample;

internal class Agent_OpenAI_Step04_CreateFromOpenAIResponseClient
{
    public async Task Run()
    {
        var envSetting = new EnvSetting();
        // Create a ResponsesClient directly from OpenAIClient
        ResponsesClient responseClient = new OpenAIClient(new ApiKeyCredential(envSetting.OPENAI_API_KEY),
            new OpenAIClientOptions
            {
                Endpoint = new Uri("https://api.deepseek.com"),
            }).GetResponsesClient();

        // Create an agent directly from the ResponsesClient using OpenAIResponseClientAgent
        OpenAIResponseClientAgent agent = new(responseClient, instructions: "You are good at telling jokes.", name: "Joker", model: envSetting.OPENAI_CHAT_MODEL_NAME);

        ResponseItem userMessage = ResponseItem.CreateUserMessageItem("给我讲一个关于海盗的笑话.");

        // Invoke the agent and output the text result.
        ResponseResult response = await agent.RunAsync([userMessage]);
        Console.WriteLine(response.GetOutputText());

        // Invoke the agent with streaming support.
        IAsyncEnumerable<StreamingResponseUpdate> responseUpdates = agent.RunStreamingAsync([userMessage]);
        await foreach (StreamingResponseUpdate responseUpdate in responseUpdates)
        {
            if (responseUpdate is StreamingResponseOutputTextDeltaUpdate textUpdate)
            {
                Console.WriteLine(textUpdate.Delta);
            }
        }
    }
}
