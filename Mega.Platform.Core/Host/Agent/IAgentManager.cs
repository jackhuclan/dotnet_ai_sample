using Microsoft.Agents.AI;

namespace Mega.Platform.Core.Host.Agent;

public interface IAgentManager
{
    AIAgent GetOrCreateAgent(string agentId, Func<AIAgent> createAgent);
}