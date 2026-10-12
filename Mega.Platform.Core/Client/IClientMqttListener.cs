using MQTTnet.Protocol;

namespace Mega.Platform.Core.Client;

/// <summary>
/// 对mqtt连接进行监听和处理
/// </summary>
public interface IClientMqttListener
{
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
    /// <param name="remoteCommand">远程指令</param>
    /// <param name="mqttQualityOfServiceLevel"></param>
    /// <returns></returns>
    Task SubscribeService(IRemoteCommand remoteCommand, MqttQualityOfServiceLevel mqttQualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce);

    /// <summary>
    /// 订阅服务及回调方法
    /// </summary>
    /// <param name="commandPath">指令请求路径</param>
    /// <param name="commandFunction">指令</param>
    /// <param name="mqttQualityOfServiceLevel"></param>
    /// <returns></returns>
    Task SubscribeService(string commandPath, CommandFunction commandFunction, MqttQualityOfServiceLevel mqttQualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce);
}
