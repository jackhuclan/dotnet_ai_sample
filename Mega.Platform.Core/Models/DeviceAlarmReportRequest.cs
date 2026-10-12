using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Mega.Platform.Core.Models;

/// <summary>
/// 设备告警上报
/// </summary>
public class DeviceAlarmReportRequest
{
    /// <summary>
    /// 唯一标识
    /// </summary>
    public long Id { get; set; } = 0;
    /// <summary>
    /// 设备产品
    /// </summary>
    public string ProductId { get; set; } = string.Empty;
    /// <summary>
    /// 设备产品
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>
    /// 设备产品
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
    /// <summary>
    /// 关联事件ID
    /// </summary>
    public string EventId { get; set; } = string.Empty;
    /// <summary>
    /// 关联某次事件TraceID
    /// </summary>
    public string TraceId { get; set; } = string.Empty;
    /// <summary>
    /// 关联事件名称
    /// </summary>
    public string EventName { get; set; } = string.Empty;
    /// <summary>
    /// 设备种类
    /// </summary>
    public DeviceKind RequestDeviceKind { get; set; } = DeviceKind.Unknown;
    /// <summary>
    /// 携带的板料
    /// </summary>
    public PanelList PayloadPanels { get; set; } = new();
    /// <summary>
    /// 携带的刀具信息
    /// </summary>
    public CutterTrays CutterTrays { get; set; } = new();
    /// <summary>
    /// 告警信息类别
    /// </summary>
    public AlarmKind AlarmKind { get; set; } = AlarmKind.Unknown;

    /// <summary>
    /// 告警id
    /// </summary>
    public string AlarmCode { get; set; } = string.Empty;
    /// <summary>
    /// 告警名称
    /// </summary>
    public string AlarmName { get; set; } = string.Empty;
    /// <summary>
    /// 告警内容
    /// </summary>
    public string AlarmContent { get; set; } = string.Empty;
    /// <summary>
    /// 告警级别
    /// </summary>
    public AlarmLevel AlarmLevel { get; set; } = AlarmLevel.Information;
    /// <summary>
    /// 告警时间
    /// </summary>
    public DateTime AlarmTime { get; set; } = DateTime.Now;
    /// <summary>
    /// 是否被处理,0未处理,1已处理,2正在处理
    /// </summary>
    public int Handled { get; set; } = 0;
    /// <summary>
    /// 告警数据
    /// </summary>
    public Dictionary<string, object> Params { get; set; } = new();

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        });
    }
}
