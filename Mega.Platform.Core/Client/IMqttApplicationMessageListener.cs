using MQTTnet;

namespace Mega.Platform.Core.Client;

/// <summary>
/// 对下发消息进行处理
/// </summary>
public interface IMqttApplicationMessageListener
{
    /// 对下发消息进行处理
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg);
}
