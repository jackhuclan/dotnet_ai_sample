//using DotNetEnv;
//using Microsoft.Agents.AI;
//using Microsoft.Agents.AI.Mem0;
//using Microsoft.Extensions.AI;
//using System.Net.Http.Headers;
//using System.Text.Json;
//using Xunit;

//namespace dotnet_ai_agent_sample.rag;

///// <summary>
///// docker run -d --name qdrant -p 6333:6333 -p 6334:6334 qdrant/qdrant
///// </summary>
//public class CustomRAGDataSourceTest : BaseTest
//{
//    [Fact]
//    public async Task TestAsync()
//    {
//        Env.Load();

//        var mem0ServiceUri = Env.GetString("MEM0_ENDPOINT") ?? throw new InvalidOperationException("MEM0_ENDPOINT is not set.");
//        var mem0ApiKey = Env.GetString("MEM0_API_KEY") ?? throw new InvalidOperationException("MEM0_API_KEY is not set.");

//        // Create an HttpClient for Mem0 with the required base address and authentication.
//        using HttpClient mem0HttpClient = new();
//        mem0HttpClient.BaseAddress = new Uri(mem0ServiceUri);
//        mem0HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", mem0ApiKey);


//        // WARNING: DefaultAzureCredential is convenient for development but requires careful consideration in production.
//        // In production, consider using a specific credential (e.g., ManagedIdentityCredential) to avoid
//        // latency issues, unintended credential probing, and potential security risks from fallback mechanisms.
//        AIAgent agent = GetChatClient()
//            .AsAIAgent(new ChatClientAgentOptions()
//            {
//                ChatOptions = new() { ModelId = OPENAI_CHAT_MODEL_NAME, Instructions = "You are a friendly travel assistant. Use known memories about the user when responding, and do not invent details." },
//                // The stateInitializer can be used to customize the Mem0 scope per session and it will be called each time a session
//                // is encountered by the Mem0Provider that does not already have Mem0Provider state stored on the session.
//                // If each session should have its own Mem0 scope, you can create a new id per session via the stateInitializer, e.g.:
//                // new Mem0Provider(mem0HttpClient, stateInitializer: _ => new(new Mem0ProviderScope() { ThreadId = Guid.NewGuid().ToString() }))
//                // In our case we are storing memories scoped by application and user instead so that memories are retained across threads.
//                AIContextProviders = [new Mem0Provider(mem0HttpClient, stateInitializer: _ => new(new Mem0ProviderScope() { ApplicationId = "getting-started-agents", UserId = "sample-user" }))]
//            });

//        AgentSession session = await agent.CreateSessionAsync();

//        // Clear any existing memories for this scope to demonstrate fresh behavior.
//        // Note that the ClearStoredMemoriesAsync method will clear memories
//        // using the scope stored in the session, or provided via the stateInitializer.
//        Mem0Provider mem0Provider = agent.GetService<Mem0Provider>()!;
//        await mem0Provider.ClearStoredMemoriesAsync(session);

//        Console.WriteLine(await agent.RunAsync("Hi there! My name is Taylor and I'm planning a hiking trip to Patagonia in November.", session));
//        Console.WriteLine(await agent.RunAsync("I'm travelling with my sister and we love finding scenic viewpoints.", session));

//        Console.WriteLine("\nWaiting briefly for Mem0 to index the new memories...\n");
//        await Task.Delay(TimeSpan.FromSeconds(2));

//        Console.WriteLine(await agent.RunAsync("What do you already know about my upcoming trip?", session));

//        Console.WriteLine("\n>> Serialize and deserialize the session to demonstrate persisted state\n");
//        JsonElement serializedSession = await agent.SerializeSessionAsync(session);
//        AgentSession restoredSession = await agent.DeserializeSessionAsync(serializedSession);
//        Console.WriteLine(await agent.RunAsync("Can you recap the personal details you remember?", restoredSession));

//        Console.WriteLine("\n>> Start a new session that shares the same Mem0 scope\n");
//        AgentSession newSession = await agent.CreateSessionAsync();
//        Console.WriteLine(await agent.RunAsync("Summarize what you already know about me.", newSession));
//    }


//}
