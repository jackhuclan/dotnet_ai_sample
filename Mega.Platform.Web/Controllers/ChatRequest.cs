using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

namespace Mega.Platform.Web.Controllers;

public class ChatRequest
{
    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("messages")]
    [JsonConverter(typeof(ChatMessageListJsonConverter))]
    public IList<ChatMessage> Messages { get; set; } = [];

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
}