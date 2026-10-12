using Mega.Platform.Core.Models;

namespace Mega.Platform.Core.Host;

public interface IDeviceServiceInvoker
{
    /// <summary>
    /// 下发服务请求给设备端
    /// </summary>
    /// <param name="request">服务请求</param>
    /// <param name="retryCount">重试次数</param>
    /// <returns></returns>
    Task<DeviceServiceInvokeResponse> InvokeService(DeviceServiceInvokeRequest request, int retryCount = 10);
}
