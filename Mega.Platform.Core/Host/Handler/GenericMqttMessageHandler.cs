using MQTTnet;

namespace Mega.Platform.Core.Host.Handler;

internal class GenericMqttMessageHandler : IMqttMessageHandler
{
    public Task<object> Handle(MqttApplicationMessageReceivedEventArgs applicationMessageArgs) => throw new NotImplementedException();
}
