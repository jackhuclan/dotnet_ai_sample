using Mega.Platform.Core.Models;

namespace Mega.Platform.Core.Client.Command;

/// <summary>
/// 服务调用指令
/// </summary>
/// <typeparam name="TDevice"></typeparam>
public abstract class AbstractCommand<TDevice> : IRemoteCommand
    where TDevice : IDevice
{
    public AbstractCommand(IServiceProvider serviceProvider,
        TDevice device,
        CommandDescriptor commandDescriptor)
    {
        Descriptor = commandDescriptor;
    }

    public CommandDescriptor Descriptor { get; }

    public abstract Task<DeviceServiceInvokeResponse> Invoke(DeviceServiceInvokeRequest deviceServiceInvokeRequest);
}
