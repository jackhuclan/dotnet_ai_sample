using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Mega.Platform.Core.Models;

/// <summary>
/// 设备告警上报返回结果
/// </summary>
public class DeviceAlarmReportResponse
{
    /// <summary>
    /// 返回code
    /// </summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>
    /// 返回message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// 服务端接收到告警，返回给客户端一个token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        });
    }
}
