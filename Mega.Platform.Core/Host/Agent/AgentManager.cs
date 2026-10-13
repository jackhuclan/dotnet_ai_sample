using Microsoft.Agents.AI;
using System.Collections.Concurrent;

namespace Mega.Platform.Core.Host.Agent;

public class AgentManager : IAgentManager
{
    private ConcurrentDictionary<string, AIAgent> _agents = new();

    public AIAgent GetOrCreateAgent(string agentId, Func<AIAgent> createAgent)
    {
        return _agents.GetOrAdd(agentId, _ => createAgent());
    }
}
