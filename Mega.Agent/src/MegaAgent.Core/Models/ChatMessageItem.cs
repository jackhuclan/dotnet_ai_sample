using System.Text.Json.Serialization;

namespace MegaAgent.Core.Models;

/// <summary>单条聊天消息。</summary>
public class ChatMessageItem
{
    /// <summary>user / assistant</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
