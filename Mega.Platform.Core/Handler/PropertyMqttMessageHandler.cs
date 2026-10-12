using MQTTnet;

namespace Mega.Platform.Core.Handler;

internal class PropertyMqttMessageHandler : IMqttMessageHandler
{
    public PropertyMqttMessageHandler() { }
    public Task<object> Handle(MqttApplicationMessageReceivedEventArgs args) => throw new NotImplementedException();
}
