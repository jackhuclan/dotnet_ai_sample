using Mega.Platform.Core.Models;

namespace Mega.Platform.Core.Client.Command;

public class DelegateCommand<TDevice> : AbstractCommand<TDevice>, IRemoteCommand
    where TDevice : IDevice
{
    private readonly CommandFunction _innerFunction;

    public DelegateCommand(IServiceProvider serviceProvider,
        TDevice device,
        CommandDescriptor commandDescriptor,
        CommandFunction innerFunction)
        : base(serviceProvider, device, commandDescriptor)
    {
        _innerFunction = innerFunction;
    }

    public override Task<DeviceServiceInvokeResponse> Invoke(DeviceServiceInvokeRequest deviceServiceInvokeRequest)
        => _innerFunction.Invoke(deviceServiceInvokeRequest);
}
