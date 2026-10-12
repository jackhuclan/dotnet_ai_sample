using System.ComponentModel;

namespace Mega.Platform.Core.Models;

/// <summary>
/// 告警级别（0通知、1告警、2紧急、3严重）
/// </summary>
public enum AlarmLevel
{
    /// <summary>
    /// 通知
    /// </summary>
    [Description("通知")]
    Information = 0,
    /// <summary>
    /// 1普通
    /// </summary>
    [Description("普通")]
    Warning = 1,
    /// <summary>
    /// 2紧急
    /// </summary>
    [Description("紧急")]
    Urgent = 2,
    /// <summary>
    /// 3严重
    /// </summary>
    [Description("严重")]
    Severe = 3,
}
