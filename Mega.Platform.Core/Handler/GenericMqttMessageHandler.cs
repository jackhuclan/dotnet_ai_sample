using MQTTnet;

namespace Mega.Platform.Core.Handler;

internal class GenericMqttMessageHandler : IMqttMessageHandler
{
    public Task<object> Handle(MqttApplicationMessageReceivedEventArgs applicationMessageArgs) => throw new NotImplementedException();
}
