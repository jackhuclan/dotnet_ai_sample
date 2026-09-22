using dotnet_ai_agent_sample.workflow;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace dotnet_ai_agent_sample;

public class Agent_Step02_StructuredOutput : BaseTest
{
    [Fact]
    public async Task TestMethod1()
    {
        var agent = GetChatClient()
            .AsAIAgent(new ChatClientAgentOptions()
            {
                ChatOptions = new()
                {
                    ModelId = OPENAI_CHAT_MODEL_NAME, // deepseek-pro
                    // 重点：换成 JsonObject，不要 ForJsonSchema<T>()
                    ResponseFormat = ChatResponseFormat.Json,
                    // Prompt 里明确指定输出PersonInfo的JSON结构，必须带上json关键字（DeepSeek官方要求）
                    Instructions = @"extract person information from the input text.
        Return only pure JSON object matching the PersonInfo schema, no extra text.
        The JSON must contain: name(string), age(int), city(string).
        Output valid json."
                }
            });

        var result = await agent.RunAsync("My name is Zhang San, I am 18 years old, and I live in Beijing.");

        // 手动反序列化
        var json = result.Text;
        var personInfo = JsonSerializer.Deserialize<PersonInfo>(json);

        Assert.True(true);
    }

    [Fact]
    public async Task UseStructuredOutputWithResponseFormatAsync()
    {
        var aiProjectClient = GetChatClient();
        Console.WriteLine("=== Structured Output with ResponseFormat ===");

        // Create the agent
        AIAgent agent = aiProjectClient.AsAIAgent(new ChatClientAgentOptions()
        {
            Name = "HelpfulAssistant",
            ChatOptions = new()
            {
                ModelId = OPENAI_CHAT_MODEL_NAME,
                Instructions = "You are a helpful assistant.",
                // Specify CityInfo as the type parameter of ForJsonSchema to indicate the expected structured output from the agent.
                ResponseFormat = ChatResponseFormat.ForJsonSchema<CityInfo>()
            }
        });

        // Invoke the agent with some unstructured input to extract the structured information from.
        AgentResponse response = await agent.RunAsync("Provide information about the capital of France.");

        // Access the structured output via the Text property of the agent response as JSON in scenarios when JSON as text is required
        // and no object instance is needed (e.g., for logging, forwarding to another service, or storing in a database).
        Console.WriteLine("Assistant Output (JSON):");
        Console.WriteLine(response.Text);
        Console.WriteLine();

        // Deserialize the JSON text to work with the structured object in scenarios when you need to access properties,
        // perform operations, or pass the data to methods that require the typed object instance.
        CityInfo cityInfo = JsonSerializer.Deserialize<CityInfo>(response.Text)!;

        Console.WriteLine("Assistant Output (Deserialized):");
        Console.WriteLine($"Name: {cityInfo.Name}");
        Console.WriteLine();
    }

    [Fact]
    public async Task UseStructuredOutputWithRunAsync()
    {
        var aiProjectClient = GetChatClient();
        Console.WriteLine("=== Structured Output with RunAsync<T> ===");

        // Create the agent
        AIAgent agent = aiProjectClient.AsAIAgent(name: "HelpfulAssistant", instructions: "You are a helpful assistant.");

        // Set CityInfo as the type parameter of RunAsync method to specify the expected structured output from the agent and invoke it with some unstructured input.
        AgentResponse<CityInfo> response = await agent.RunAsync<CityInfo>("Provide information about the capital of France.");

        // Access the structured output via the Result property of the agent response.
        CityInfo cityInfo = response.Result;

        Console.WriteLine("Assistant Output:");
        Console.WriteLine($"Name: {cityInfo.Name}");
        Console.WriteLine();
    }

    [Fact]
    public async Task UseStructuredOutputWithRunStreamingAsync()
    {
        var aiProjectClient = GetChatClient();
        Console.WriteLine("=== Structured Output with RunStreamingAsync ===");

        // Create the agent
        AIAgent agent = aiProjectClient.AsAIAgent(new ChatClientAgentOptions()
        {
            Name = "HelpfulAssistant",
            ChatOptions = new()
            {
                ModelId = OPENAI_CHAT_MODEL_NAME,
                Instructions = "You are a helpful assistant.",
                // Specify CityInfo as the type parameter of ForJsonSchema to indicate the expected structured output from the agent.
                ResponseFormat = ChatResponseFormat.ForJsonSchema<CityInfo>()
            }
        });

        // Invoke the agent with some unstructured input while streaming, to extract the structured information from.
        IAsyncEnumerable<AgentResponseUpdate> updates = agent.RunStreamingAsync("Provide information about the capital of France.");

        // Assemble all the parts of the streamed output.
        AgentResponse nonGenericResponse = await updates.ToAgentResponseAsync();

        // Access the structured output by deserializing JSON in the Text property.
        CityInfo cityInfo = JsonSerializer.Deserialize<CityInfo>(nonGenericResponse.Text)!;

        Console.WriteLine("Assistant Output:");
        Console.WriteLine($"Name: {cityInfo.Name}");
        Console.WriteLine();
    }

    [Fact]
    public async Task UseStructuredOutputWithMiddlewareAsync()
    {
        var meaiChatClient = GetChatClient();
        Console.WriteLine("=== Structured Output with UseStructuredOutput Middleware ===");

        // Create chat client that will transform the agent text response into structured output.
        //IChatClient meaiChatClient = aiProjectClient.GetProjectOpenAIClient()
        //    .GetProjectResponsesClientForModel(deploymentName)
        //    .AsIChatClientWithStoredOutputDisabled(deploymentName);

        // Create the agent
        AIAgent agent = meaiChatClient.AsAIAgent(name: "HelpfulAssistant", instructions: "You are a helpful assistant.");

        // Add structured output middleware via UseStructuredOutput method to add structured output support to the agent.
        // This middleware transforms the agent's text response into structured data using a chat client.
        // Since our agent does support structured output natively, we will add a middleware that removes ResponseFormat
        //  from the AgentRunOptions to emulate an agent that doesn't support structured output natively
        agent = agent
            .AsBuilder()
            .UseStructuredOutput(meaiChatClient)
            .Use(ResponseFormatRemovalMiddleware, null)
            .Build();

        // Set CityInfo as the type parameter of RunAsync method to specify the expected structured output from the agent and invoke it with some unstructured input.
        AgentResponse<CityInfo> response = await agent.RunAsync<CityInfo>("Provide information about the capital of France.");

        // Access the structured output via the Result property of the agent response.
        CityInfo cityInfo = response.Result;

        Console.WriteLine("Assistant Output:");
        Console.WriteLine($"Name: {cityInfo.Name}");
        Console.WriteLine();
    }

    static Task<AgentResponse> ResponseFormatRemovalMiddleware(IEnumerable<ChatMessage> messages, AgentSession? session, AgentRunOptions? options, AIAgent innerAgent, CancellationToken cancellationToken)
    {
        // Remove any ResponseFormat from the options to emulate an agent that doesn't support structured output natively.
        options = options?.Clone();
        options?.ResponseFormat = null;

        return innerAgent.RunAsync(messages, session, options, cancellationToken);
    }
}

/// <summary>
/// Represents information about a city, including its name.
/// </summary>
[Description("Information about a city")]
public sealed class CityInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}