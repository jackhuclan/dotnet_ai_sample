using Mega.Platform.Core.Client.Command;

namespace Mega.Platform.Core.Client;

public interface IDevice
{
    string ProductId { get; }
    string DeviceId { get; }
    string DeviceName { get; }
    string ClientId { get; }
    IObjectFactory ObjectFactory { get; }
    IMqttClientWrapper MqttClientWrapper { get; }
    public ICommandManager CommandManager { get; }
    /// <summary>
    /// 全局服务
    /// </summary>
    IServiceProvider ApplicationServices { get; }
    /// <summary>
    /// 设备描述符
    /// </summary>
    DeviceDescriptor DeviceDescriptor { get; }
    void AddCommand(IRemoteCommand remoteCommand);
    void AddCommand(CommandDescriptor commandDescriptor, CommandFunction command, CommandUsageScope commandUsage = CommandUsageScope.Both);
}
