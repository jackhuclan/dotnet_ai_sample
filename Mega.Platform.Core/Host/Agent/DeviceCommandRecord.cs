using Mega.Platform.Core.Models;

namespace Mega.Platform.Core.Host.Agent;

internal record DeviceCommandRecord
{
    /// <summary>
    /// 设备ID
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>
    /// 命令ID/tool id
    /// </summary>
    public string CommandId { get; set; } = string.Empty;

    public static DeviceCommandRecord From(DeviceCommandRequest commandRequest)
    {
        return new DeviceCommandRecord
        {
            DeviceId = commandRequest.DeviceId,
            CommandId = commandRequest.CommandId
        };
    }

    public override string ToString()
    {
        return $"{DeviceId}_{CommandId}";
    }
}
