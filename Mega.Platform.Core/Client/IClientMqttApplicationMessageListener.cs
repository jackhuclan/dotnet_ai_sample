using System.Collections.Concurrent;

namespace Mega.Platform.Core.Client;

/// <summary>
/// 对下发消息进行处理
/// </summary>
public interface IClientMqttApplicationMessageListener
{
    /// <summary>
    /// 监听的topic和对应的服务
    /// </summary>
    ConcurrentDictionary<string, CommandFunction> Services { get; }

    /// <summary>
    /// 对下发消息进行处理
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg);
}
