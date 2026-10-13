using Mega.Platform.Core.Client;
using Microsoft.Extensions.Options;

namespace Mega.Blade;

internal class AOIDevice : DefaultDevice
{
    public AOIDevice(IOptions<DeviceDescriptor> deviceDescriptorOptions, IServiceProvider serviceProvider)
        : base(deviceDescriptorOptions, serviceProvider)
    {
    }
}
