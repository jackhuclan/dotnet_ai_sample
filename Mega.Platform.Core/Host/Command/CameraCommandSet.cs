using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mega.Platform.Core.Host.Command;

public class CameraCommandSet : IMqttFunctionTools
{
    private readonly IDeviceServiceInvoker _deviceServiceInvoker;

    public CameraCommandSet(IDeviceServiceInvoker deviceServiceInvoker,
       ILoggerFactory loggerFactory)
    {
        _deviceServiceInvoker = deviceServiceInvoker;
    }

    public CommandFunction OpenFunc => req => _deviceServiceInvoker.InvokeService(req);
    public CommandFunction CloseFunc => req => _deviceServiceInvoker.InvokeService(req);

    public AITool[] AsTools()
    {
        return
        [
            AIFunctionFactory.Create(this.OpenFunc, "camera_open", "打开相机"),
            AIFunctionFactory.Create(this.CloseFunc, "camera_close", "关闭相机")
        ];
    }
}
