using Mega.Platform.Core.Models;

namespace Mega.Platform.Core.Client.Command;

/// <summary>
/// 远程控制指令接口
/// </summary>
public interface IRemoteCommand
{
    CommandDescriptor Descriptor { get; }

    Task<DeviceServiceInvokeResponse> Invoke(DeviceServiceInvokeRequest deviceServiceInvokeRequest);
}
