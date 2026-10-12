using MQTTnet;

namespace Mega.Platform.Core.Handler;

internal class EventMqttMessageHandler : IMqttMessageHandler
{
    public Task<object> Handle(MqttApplicationMessageReceivedEventArgs applicationMessageArgs) => throw new NotImplementedException();
}
