using MQTTnet;
using MQTTnet.Protocol;

namespace Mega.Platform.Core.Host;

internal class MqttMessageDispatcher : IMqttMessageDispatcher
{
    private readonly Dictionary<string, IMqttMessageHandler> services = new();
    private readonly IMqttClient _mqttClient;
    private readonly IMqttMessageHandlerFactory _agentHandlerFactory;

    public MqttMessageDispatcher(IMqttClient mqttClient,
        IMqttMessageHandlerFactory agentHandlerFactory)
    {
        _mqttClient = mqttClient;
        _agentHandlerFactory = agentHandlerFactory;
        services.Add(MqttTopics.ALL, _agentHandlerFactory.CreateHandler(MqttTopics.ALL));
    }

    public IMqttMessageHandler this[string topic]
    {
        get { return services[topic]; }
    }

    public async Task Start()
    {
        await SubscribeService(MqttTopics.GetSubscribeTopic(MqttTopics.OnlineTopicTemplate), _agentHandlerFactory.CreateHandler(MqttTopics.OnlineTopicTemplate));
        await SubscribeService(MqttTopics.GetSubscribeTopic(MqttTopics.EventTopicTemplate), _agentHandlerFactory.CreateHandler(MqttTopics.EventTopicTemplate));
        await SubscribeService(MqttTopics.GetSubscribeTopic(MqttTopics.StatusTopicTemplate), _agentHandlerFactory.CreateHandler(MqttTopics.StatusTopicTemplate));
        await SubscribeService(MqttTopics.GetSubscribeTopic(MqttTopics.AlarmTopicTemplate), _agentHandlerFactory.CreateHandler(MqttTopics.AlarmTopicTemplate));
        await SubscribeService(MqttTopics.GetSubscribeTopic(MqttTopics.PropertyTopicTemplate), _agentHandlerFactory.CreateHandler(MqttTopics.PropertyTopicTemplate));
    }

    public IMqttMessageHandler SelectHandler(string topic)
    {
        var subscribeTopics = new List<string>()
        {
            //Topics.Downstream.GetSubscribeTopic(Topics.Downstream.ServiceInvokeTopicTemplate),//pass through service inteception
            MqttTopics.GetSubscribeTopic(MqttTopics.OnlineTopicTemplate),
            MqttTopics.GetSubscribeTopic(MqttTopics.EventTopicTemplate),
            MqttTopics.GetSubscribeTopic(MqttTopics.StatusTopicTemplate),
            MqttTopics.GetSubscribeTopic(MqttTopics.AlarmTopicTemplate),
            MqttTopics.GetSubscribeTopic(MqttTopics.PropertyTopicTemplate)
        };

        foreach (var subscribeTopic in subscribeTopics)
        {
            if (MqttTopicFilterComparer.Compare(topic, subscribeTopic) == MqttTopicFilterCompareResult.IsMatch)
            {
                return services[subscribeTopic];
            }
        }

        return services[MqttTopics.ALL];
    }

    private async Task SubscribeService(string serviceTopic, IMqttMessageHandler? mqttMessageHandler)
    {
        if (string.IsNullOrEmpty(serviceTopic)) return;
        if (mqttMessageHandler == null) return;

        if (services.TryAdd(serviceTopic, mqttMessageHandler))
        {
            await _mqttClient.SubscribeAsync(serviceTopic, MqttQualityOfServiceLevel.AtLeastOnce);
        }
    }
}
