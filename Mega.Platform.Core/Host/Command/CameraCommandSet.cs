using Mega.Platform.Core.Models;
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

    /// <summary>
    /// 打开相机
    /// </summary>
    public async Task<DeviceServiceInvokeResponse> Open(DeviceServiceInvokeRequest deviceServiceInvokeRequest)
    {
        return await _deviceServiceInvoker.InvokeService(deviceServiceInvokeRequest);
    }

    /// <summary>
    /// 关闭相机
    /// </summary>
    public async Task<DeviceServiceInvokeResponse> Close(DeviceServiceInvokeRequest deviceServiceInvokeRequest)
    {
        return await _deviceServiceInvoker.InvokeService(deviceServiceInvokeRequest);
    }

    //SetTriggerMode、SetTriggerSource、SoftTrigger、SetTriggerLine、SetLineDebouncerTime

    public AITool[] AsTools()
    {
        return
        [
            AIFunctionFactory.Create(this.Open, "camera_open", "打开相机"),
            AIFunctionFactory.Create(this.Close, "camera_close", "关闭相机")
        ];
    }
}
