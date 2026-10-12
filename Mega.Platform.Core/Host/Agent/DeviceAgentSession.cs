using Microsoft.Agents.AI;

namespace Mega.Platform.Core.Host.Agent;

public class DeviceAgentSession : AgentSession
{
    public string SessionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastAccessedAt { get; set; } = DateTime.Now;
    public DeviceAgentSession(string sessionId, string title, string productId, string deviceId, string userId)
    {
        SessionId = sessionId;
        Title = title;
        ProductId = productId;
        DeviceId = deviceId;
        UserId = userId;
    }
}
