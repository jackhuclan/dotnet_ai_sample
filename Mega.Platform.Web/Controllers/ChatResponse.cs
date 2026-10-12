using System.Text.Json.Serialization;

namespace Mega.Platform.Web.Controllers;

public class ChatResponse
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
}
