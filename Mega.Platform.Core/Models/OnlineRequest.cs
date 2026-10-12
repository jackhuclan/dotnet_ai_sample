using System.Text.Json;

namespace Mega.Platform.Core.Models;

public class OnlineRequest
{
    public string ProductId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public bool Connected { get; set; }
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
