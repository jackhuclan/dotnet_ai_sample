using Mega.Platform.Core.Models;

namespace Mega.Platform.Core.Client.Command;

/// <summary>
/// 指令函数
/// </summary>
/// <param name="deviceServiceInvokeRequest">传入参数</param>
/// <returns>返回DeviceServiceInvokeResponse</returns>

public delegate Task<DeviceServiceInvokeResponse> CommandFunction(DeviceServiceInvokeRequest deviceServiceInvokeRequest);
