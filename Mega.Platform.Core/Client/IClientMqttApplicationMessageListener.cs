using Mega.Platform.Core.Client.Command;
using MQTTnet;
using MQTTnet.Protocol;
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
    /// 设备连上mqtt
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    Task OnConnected(IDevice device);

    /// <summary>
    /// 设备与mqtt断开连接
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    Task OnDisonnected(IDevice device);

    /// <summary>
    /// 上报心跳事件
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    Task<bool> OnHeartbeat(IDevice device);

    /// <summary>
    /// 订阅服务及回调方法
    /// </summary>
    /// <param name="device"></param>
    /// <param name="remoteCommand">远程指令</param>
    /// <param name="mqttQualityOfServiceLevel"></param>
    /// <returns></returns>
    Task SubscribeService(IDevice device, IRemoteCommand remoteCommand, MqttQualityOfServiceLevel mqttQualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce);

    /// <summary>
    /// 订阅服务及回调方法
    /// </summary>
    /// <param name="device"></param>
    /// <param name="commandPath">指令请求路径</param>
    /// <param name="commandFunction">指令</param>
    /// <param name="mqttQualityOfServiceLevel"></param>
    /// <returns></returns>
    Task SubscribeService(IDevice device, string commandPath, CommandFunction commandFunction, MqttQualityOfServiceLevel mqttQualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce);

    /// <summary>
    /// 对下发消息进行处理
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg);
}
