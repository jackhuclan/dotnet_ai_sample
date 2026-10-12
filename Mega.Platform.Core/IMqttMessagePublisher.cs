namespace Mega.Platform.Core;

/// <summary>
/// mqtt message publisher, 具备请求和获取回复消息功能
/// </summary>
public interface IMqttMessagePublisher
{
    /// <summary>
    /// request/reply模式向mqtt server请求消息并等待返回结果
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="requestTopic">请求消息的topic</param>
    /// <param name="responseTopic">返回消息的topic</param>
    /// <param name="request">请求内容</param>
    /// <param name="mqttQualityOfServiceLevel">Qos</param>
    /// <param name="cancellationToken">取消token</param>
    /// <returns></returns>
    Task<TResponse?> Publish<TRequest, TResponse>(string requestTopic,
        string responseTopic,
        TRequest request,
        MqttQualityOfServiceLevel mqttQualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce,
        CancellationToken cancellationToken = default);

    Task<MqttClientPublishResult> PublishBinaryAsync(string topic, IEnumerable<byte> payload = null);
    Task<MqttClientPublishResult> PublishBinaryAsync(string topic, IEnumerable<byte> payload = null, MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false, CancellationToken cancellationToken = default);
    Task<MqttClientPublishResult> PublishStringAsync(string topic, string payload = null);
    Task<MqttClientPublishResult> PublishStringAsync(string topic, string payload = null, MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false, CancellationToken cancellationToken = default);
}
