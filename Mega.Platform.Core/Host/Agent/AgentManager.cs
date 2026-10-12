using System.Collections.Concurrent;

namespace Mega.Platform.Core.Host.Agent;

public class AgentManager : IAgentManager
{
    private ConcurrentDictionary<string, DeviceChatAgent> _agents = new();

    public DeviceChatAgent GetOrCreateAgent(string agentId, Func<DeviceChatAgent> createAgent)
    {
        return _agents.GetOrAdd(agentId, _ => createAgent());
    }
}
