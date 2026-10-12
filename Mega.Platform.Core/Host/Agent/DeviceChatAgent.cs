using Mega.Platform.Core.Client;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Mega.Platform.Core.Host.Agent;

public sealed class DeviceChatAgent : DelegatingAIAgent
{
    public override string? Name => "DeviceChatAgent";

    private readonly ChatHistoryProvider _chatHistoryProvider = new InMemoryChatHistoryProvider();
    private readonly DeviceDescriptor _deviceDescriptor;

    public DeviceChatAgent(AIAgent innerAgent, DeviceDescriptor deviceDescriptor) : base(innerAgent)
    {
        _deviceDescriptor = deviceDescriptor;
    }

    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
        => new(new DeviceAgentSession());

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(AgentSession session, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
    {
        if (session is not DeviceAgentSession typedSession)
        {
            throw new ArgumentException($"The provided session is not of type {nameof(DeviceAgentSession)}.", nameof(session));
        }

        return new(JsonSerializer.SerializeToElement(typedSession, jsonSerializerOptions));
    }

    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(JsonElement serializedState, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
        => new(serializedState.Deserialize<DeviceAgentSession>(jsonSerializerOptions)!);

    protected override async Task<AgentResponse> RunCoreAsync(IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        // Create a session if the user didn't supply one.
        session ??= await this.CreateSessionAsync(cancellationToken);

        if (session is not DeviceAgentSession typedSession)
        {
            throw new ArgumentException($"The provided session is not of type {nameof(DeviceAgentSession)}.", nameof(session));
        }

        // Get existing messages from the store
        var invokingContext = new ChatHistoryProvider.InvokingContext(this, session, messages);
        var userAndChatHistoryMessages = await this._chatHistoryProvider.InvokingAsync(invokingContext, cancellationToken);

        // Clone the input messages and turn them into response messages with upper case text.
        List<ChatMessage> responseMessages = InnerAgent.RunAsync(messages, this.Name).ToList();

        // Notify the session of the input and output messages.
        var invokedContext = new ChatHistoryProvider.InvokedContext(this, session, userAndChatHistoryMessages, responseMessages);
        await this._chatHistoryProvider.InvokedAsync(invokedContext, cancellationToken);

        return new AgentResponse
        {
            AgentId = this.Id,
            ResponseId = Guid.NewGuid().ToString("N"),
            Messages = responseMessages
        };
    }

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Create a session if the user didn't supply one.
        session ??= await this.CreateSessionAsync(cancellationToken);

        if (session is not DeviceAgentSession typedSession)
        {
            throw new ArgumentException($"The provided session is not of type {nameof(DeviceAgentSession)}.", nameof(session));
        }

        // Get existing messages from the store
        var invokingContext = new ChatHistoryProvider.InvokingContext(this, session, messages);
        var userAndChatHistoryMessages = await this._chatHistoryProvider.InvokingAsync(invokingContext, cancellationToken);

        // Clone the input messages and turn them into response messages with upper case text.
        List<ChatMessage> responseMessages = CloneAndToUpperCase(messages, this.Name).ToList();

        // Notify the session of the input and output messages.
        var invokedContext = new ChatHistoryProvider.InvokedContext(this, session, userAndChatHistoryMessages, responseMessages);
        await this._chatHistoryProvider.InvokedAsync(invokedContext, cancellationToken);

        foreach (var message in responseMessages)
        {
            yield return new AgentResponseUpdate
            {
                AgentId = this.Id,
                AuthorName = message.AuthorName,
                Role = ChatRole.Assistant,
                Contents = message.Contents,
                ResponseId = Guid.NewGuid().ToString("N"),
                MessageId = Guid.NewGuid().ToString("N")
            };
        }
    }

    private static IEnumerable<ChatMessage> CloneAndToUpperCase(IEnumerable<ChatMessage> messages, string? agentName) => messages.Select(x =>
    {
        // Clone the message and update its author to be the agent.
        var messageClone = x.Clone();
        messageClone.Role = ChatRole.Assistant;
        messageClone.MessageId = Guid.NewGuid().ToString("N");
        messageClone.AuthorName = agentName;

        // Clone and convert any text content to upper case.
        messageClone.Contents = x.Contents.Select(c => c switch
        {
            TextContent tc => new TextContent(tc.Text.ToUpperInvariant())
            {
                AdditionalProperties = tc.AdditionalProperties,
                Annotations = tc.Annotations,
                RawRepresentation = tc.RawRepresentation
            },
            _ => c
        }).ToList();

        return messageClone;
    });
}
