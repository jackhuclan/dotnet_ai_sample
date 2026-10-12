namespace Mega.Platform.Core.Host.Agent;

public interface ISessionManager
{
    bool TryGetSession(string sessionId, out DeviceAgentSession session);
}