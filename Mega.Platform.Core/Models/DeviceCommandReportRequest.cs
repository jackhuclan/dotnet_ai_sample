using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Mega.Platform.Core.Models;

public record DeviceCommandReportRequest
{
    /// <summary>
    /// 产品ID
    /// </summary>
    public string ProductId { get; set; } = string.Empty;
    /// <summary>
    /// 设备ID
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>
    /// 客户端ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
    /// <summary>
    /// 指令唯一名称，设备唯一，不同设备可以相同，snake_case（蛇形命名法）风格，比如: make_order
    /// </summary>
    public string CommandId { get; set; } = string.Empty;

    /// <summary>
    /// 指令挂载的路径: a/b/c
    /// </summary>
    public string CommandPath { get; set; } = string.Empty;
    /// <summary>
    /// 指令描述，一定要描述什么情况下使用该指令，描述清楚指令的作用和使用场景
    /// </summary>
    public string Description { get; set; } = string.Empty;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        });
    }
}
