using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Mega.Platform.Core.Models;

public class DeviceEventReportResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string? EventId { get; set; }
    public object? Data { get; set; }

    public string? TraceId { get; set; }

    public Dictionary<string, object?> Params { get; set; } = new Dictionary<string, object?>();
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        });
    }
}
