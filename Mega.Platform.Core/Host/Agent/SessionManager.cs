using System.Collections.Concurrent;

namespace Mega.Platform.Core.Host.Agent;

public class SessionManager : ISessionManager
{
    private ConcurrentDictionary<string, DeviceAgentSession> _sessions = new();

    public bool TryGetSession(string sessionId, out DeviceAgentSession session)
    {
        return _sessions.TryGetValue(sessionId, out session);
    }
}
