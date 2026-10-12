using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Mega.Platform.Core.Models;

public class DeviceServiceInvokeRequest
{
    /// <summary>
    /// source device's productId
    /// </summary>
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// source device's DeviceId
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// source device's ClientId
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// source device's ServiceId
    /// </summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>
    /// 服务下面的服务路径：module/service/method
    /// </summary>
    public string ServicePath { get; set; } = string.Empty;

    public DateTime CreateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// parameters for source device call target device
    /// </summary>
    public Dictionary<string, object?> Params { get; set; } = new Dictionary<string, object?>();

    public string RequestTopic { get; set; } = string.Empty;

    public string ReplyTopic { get; set; } = string.Empty;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        });
    }
}
