using Mega.Platform.Core.Client;

namespace Mega.Blade;

internal class AOIDevice : DefaultDevice
{
    public AOIDevice(DeviceDescriptor deviceDescriptor, IServiceProvider serviceProvider)
        : base(deviceDescriptor, serviceProvider)
    {
    }
}
