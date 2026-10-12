using Mega.Platform.Core.Models;

namespace Mega.Platform.Core.Client.Command;

public class DefaultCommand<TDevice> : AbstractCommand<TDevice>
    where TDevice : IDevice
{
    public DefaultCommand(IServiceProvider serviceProvider,
       TDevice device,
        CommandDescriptor commandDescriptor)
       : base(serviceProvider, device, commandDescriptor)
    {
    }

    public override Task<DeviceServiceInvokeResponse> Invoke(DeviceServiceInvokeRequest deviceServiceInvokeRequest)
    {
        return Task.FromResult(DeviceServiceInvokeResponse.SUCCESS);
    }
}
