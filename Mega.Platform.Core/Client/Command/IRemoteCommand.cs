using Mega.Platform.Core.Models;

namespace Mega.Platform.Core.Client.Command;

public interface IRemoteCommand
{
    CommandDescriptor Descriptor { get; }

    Task<DeviceServiceInvokeResponse> Invoke(DeviceServiceInvokeRequest deviceServiceInvokeRequest);
}
