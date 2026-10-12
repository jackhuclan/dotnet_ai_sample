using Mega.Platform.Core.Client.Command;

namespace Mega.Platform.Core.Client;

public interface IDevice
{
    string ProductId { get; set; }
    string DeviceId { get; set; }
    string DeviceName { get; set; }
    string ClientId { get; set; }

    /// <summary>
    /// 远程控制指令集合
    /// </summary>
    CommandSet Commands { get; }
}
