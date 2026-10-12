namespace Mega.Platform.Core;

internal class MqttMessageHandlerFactory : IMqttMessageHandlerFactory
{
    private readonly IServiceProvider _provider;

    public MqttMessageHandlerFactory(IServiceProvider provider)
    {
        this._provider = provider;
    }

    public IMqttMessageHandler CreateHandler(string topic)
    {
        switch (topic)
        {
            case MqttTopics.OnlineTopicTemplate:
                return (IMqttMessageHandler)ActivatorUtilities.CreateInstance(_provider, typeof(OnlineMqttMessageHandler));
            case MqttTopics.EventTopicTemplate:
                return (IMqttMessageHandler)ActivatorUtilities.CreateInstance(_provider, typeof(EventMqttMessageHandler));
            case MqttTopics.StatusTopicTemplate:
                return (IMqttMessageHandler)ActivatorUtilities.CreateInstance(_provider, typeof(StatusMqttMessageHandler));
            case MqttTopics.AlarmTopicTemplate:
                return (IMqttMessageHandler)ActivatorUtilities.CreateInstance(_provider, typeof(AlarmMqttMessageHandler));
            case MqttTopics.PropertyTopicTemplate:
                return (IMqttMessageHandler)ActivatorUtilities.CreateInstance(_provider, typeof(PropertyMqttMessageHandler));
            default:
                return (IMqttMessageHandler)ActivatorUtilities.CreateInstance(_provider, typeof(GenericMqttMessageHandler));
        }

    }
}
