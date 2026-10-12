using Mega.Platform.Core.Models;

namespace Mega.Platform.Core.Client.Command;

public class DelegateCommand<TDevice> : AbstractCommand<TDevice>, IRemoteCommand
    where TDevice : IDevice
{
    private readonly CommandFunction _commandFunction;

    public DelegateCommand(IServiceProvider serviceProvider,
        TDevice device,
        CommandDescriptor commandDescriptor,
        CommandFunction commandFunction)
        : base(serviceProvider, device, commandDescriptor)
    {
        _commandFunction = commandFunction;
    }

    public override Task<DeviceServiceInvokeResponse> Invoke(DeviceServiceInvokeRequest deviceServiceInvokeRequest)
        => _commandFunction.Invoke(deviceServiceInvokeRequest);
}
