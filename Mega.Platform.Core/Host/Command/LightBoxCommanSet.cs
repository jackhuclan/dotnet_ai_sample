using Microsoft.Extensions.Logging;

namespace Mega.Platform.Core.Host.Command;

internal class LightBoxCommanSet
{
    private readonly IDeviceServiceInvoker _deviceServiceInvoker;

    public LightBoxCommanSet(IDeviceServiceInvoker deviceServiceInvoker,
       ILoggerFactory loggerFactory)
    {
        _deviceServiceInvoker = deviceServiceInvoker;
    }
}
