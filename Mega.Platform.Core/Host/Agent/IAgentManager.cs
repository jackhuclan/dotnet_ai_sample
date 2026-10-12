namespace Mega.Platform.Core.Host.Agent;

public interface IAgentManager
{
    DeviceChatAgent GetOrCreateAgent(string agentId, Func<DeviceChatAgent> createAgent);
}