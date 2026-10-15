using Mega.Platform.Core.Models;
using Microsoft.Extensions.AI;

namespace Mega.Platform.Core.Host.Agent;

public interface IAIToolManager
{
    void RegisterTool(DeviceCommandRequest commandRequest);
    /// <summary>
    /// 选择某个设备的可用工具集
    /// </summary>
    /// <param name="deviceId">设备id</param>
    /// <returns></returns>
    IList<AITool> Tools(string deviceId);
}