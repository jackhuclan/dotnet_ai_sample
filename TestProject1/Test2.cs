using CommunityToolkit.VectorData.InMemory;
using DotNetEnv;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OpenAI;
using System.ClientModel;
using Xunit;

namespace dotnet_ai_agent_sample;

public class Test2
{
    [Fact]
    public void TestEmbedding()
    {
        DotNetEnv.Env.Load();
        var embeddingModel = Env.GetString("OPENAI_EMBEDDING_MODEL_NAME");
        var embeddingClient = new OpenAIClient(new ApiKeyCredential("sk-token"), new OpenAIClientOptions
        {
            Endpoint = new Uri("http://localhost:11434/v1"),
        }).GetEmbeddingClient(embeddingModel);

        var clientResult = embeddingClient.GenerateEmbedding("Hello world");
        var embedding = clientResult.Value.ToFloats().ToArray();
        Assert.True(embedding.Length > 0);
    }

    [Fact]
    public async Task TestMethod1Async()
    {
        DotNetEnv.Env.Load();
        var apiKey = Env.GetString("OPENAI_API_KEY");
        var model = Env.GetString("OPENAI_CHAT_MODEL_NAME");
        var embeddingModel = Env.GetString("OPENAI_EMBEDDING_MODEL_NAME");
        var chatClient = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com"),
        }).GetChatClient(model).AsIChatClient();

        var embeddingClient = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri("http://localhost:11434"),
        }).GetEmbeddingClient(embeddingModel);

        VectorStore vectorStore = new InMemoryVectorStore(new InMemoryVectorStoreOptions()
        {
            EmbeddingGenerator = embeddingClient.AsIEmbeddingGenerator(3072),
        });

        AIAgent agent = chatClient
        .AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = new() { ModelId = model, Instructions = "You are good at telling jokes." },
            Name = "Joker",
            AIContextProviders = [new ChatHistoryMemoryProvider(
                vectorStore,
                collectionName: "chathistory",
                vectorDimensions: 3072,
                // Callback to configure the initial state of the ChatHistoryMemoryProvider.
                // The ChatHistoryMemoryProvider stores its state in the AgentSession and this callback
                // will be called whenever the ChatHistoryMemoryProvider cannot find existing state in the session,
                // typically the first time it is used with a new session.
                session => new ChatHistoryMemoryProvider.State(
                    // Configure the scope values under which chat messages will be stored.
                    // In this case, we are using a fixed user ID and a unique session ID for each new session.
                    storageScope: new() { UserId = "UID1", SessionId = Guid.NewGuid().ToString() },
                    // Configure the scope which would be used to search for relevant prior messages.
                    // In this case, we are searching for any messages for the user across all sessions.
                    searchScope: new() { UserId = "UID1" }))]
        });

        // Start a new session for the agent conversation.
        AgentSession session = await agent.CreateSessionAsync();

        // Run the agent with the session that stores conversation history in the vector store.
        Console.WriteLine(await agent.RunAsync("I like jokes about Pirates. Tell me a joke about a pirate.", session));

        // Start a second session. Since we configured the search scope to be across all sessions for the user,
        // the agent should remember that the user likes pirate jokes.
        AgentSession? session2 = await agent.CreateSessionAsync();

        // Run the agent with the second session.
        Console.WriteLine(await agent.RunAsync("Tell me a joke that I might like.", session2));
    }
}
