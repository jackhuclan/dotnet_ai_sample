using Mega.Platform.Core.Client.Command;

namespace Mega.Platform.Core.Client;

public interface IDevice
{
    string ProductId { get; set; }
    string DeviceId { get; set; }
    string DeviceName { get; set; }
    string ClientId { get; set; }
    IObjectFactory ObjectFactory { get; }
    IMqttClientWrapper MqttClientWrapper { get; }
    /// <summary>
    /// 全局服务
    /// </summary>
    IServiceProvider ApplicationServices { get; }
    /// <summary>
    /// 设备描述符
    /// </summary>
    DeviceDescriptor DeviceDescriptor { get; }
    /// <summary>
    /// 远程控制指令集合
    /// </summary>
    CommandSet Commands { get; }
    void AddCommand(IRemoteCommand remoteCommand);
    void AddCommand(CommandDescriptor commandDescriptor, CommandFunction command, CommandUsageScope commandUsage = CommandUsageScope.Both);
}
