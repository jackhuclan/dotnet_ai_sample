using System.Text.Json;

namespace Mega.Platform.Core.Models;

public class OnlineResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
