using Mega.Platform.Core.Utils;
using MQTTnet;
using MQTTnet.Protocol;
using System.Text;
using System.Text.Json;

namespace Mega.Platform.Core;

public class MqttMessagePublisher : IMqttMessagePublisher
{
    private readonly IMqttClient _mqttClient;
    private readonly IAsyncTaskWaiter _asyncTaskWaiter;

    //private AutoResetEvent _autoResetEvent = new AutoResetEvent(true);
    private volatile string _waitingKey = Guid.NewGuid().ToString("N");

    public MqttMessagePublisher(IMqttClient mqttClient, IAsyncTaskWaiter asyncTaskWaiter)
    {
        _mqttClient = mqttClient;
        _asyncTaskWaiter = asyncTaskWaiter;

        _mqttClient.ApplicationMessageReceivedAsync += (arg) =>
        {
            _asyncTaskWaiter.TrySetResult(_waitingKey, arg.ApplicationMessage.Payload.ToStr().GetBytes(Encoding.UTF8));
            return Task.CompletedTask;
        };
    }

    public Task<MqttClientPublishResult> PublishBinaryAsync(string topic, IEnumerable<byte> payload = null)
    {
        return PublishBinaryAsync(topic, payload, MqttQualityOfServiceLevel.AtMostOnce, false, default);
    }

    public Task<MqttClientPublishResult> PublishBinaryAsync(string topic,
            IEnumerable<byte> payload = null,
            MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
            bool retain = false,
            CancellationToken cancellationToken = default)
    {
        return _mqttClient.PublishBinaryAsync(topic, payload, qualityOfServiceLevel, retain, cancellationToken);
    }

    public Task<MqttClientPublishResult> PublishStringAsync(string topic, string payload = null)
    {
        return PublishStringAsync(topic, payload, MqttQualityOfServiceLevel.AtMostOnce, false, default);
    }

    public Task<MqttClientPublishResult> PublishStringAsync(string topic,
            string payload = null,
            MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
            bool retain = false,
            CancellationToken cancellationToken = default)
    {
        return _mqttClient.PublishStringAsync(topic, payload, qualityOfServiceLevel, retain, cancellationToken);
    }

    public async Task<TResponse?> Publish<TRequest, TResponse>(string requestTopic,
        string responseTopic,
        TRequest request,
        MqttQualityOfServiceLevel mqttQualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce,
        CancellationToken cancellationToken = default)
    {
        try
        {
            //_autoResetEvent.WaitOne();

            _waitingKey = Guid.NewGuid().ToString("N");

            var task = Task.Run(async () =>
            {
                var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(responseTopic)
                    .Build();

                var subscribeResult = await _mqttClient.SubscribeAsync(subscribeOptions);

                var message = new MqttApplicationMessageBuilder()
                    .WithQualityOfServiceLevel(mqttQualityOfServiceLevel)
                    .WithTopic(requestTopic)
                    .WithResponseTopic(responseTopic)
                    .WithPayload(JsonSerializer.Serialize(request))
                    .Build();

                var publishResult = await _mqttClient.PublishAsync(message);
                if (publishResult.ReasonCode != MqttClientPublishReasonCode.Success)
                {
                    throw new Exception(publishResult.ReasonString);
                }
            });

            var result = await _asyncTaskWaiter.ExecuteTaskAsWaiter(_waitingKey, task, cancellationToken);
            return JsonSerializer.Deserialize<TResponse>(Encoding.UTF8.GetString(result));
        }
        finally
        {
            //_autoResetEvent.Set();
        }
    }
}
