using MQTTnet;

namespace Mega.Platform.Core.Host.Handler;

internal class PropertyMqttMessageHandler : IMqttMessageHandler
{
    public PropertyMqttMessageHandler() { }
    public Task<object> Handle(MqttApplicationMessageReceivedEventArgs args) => throw new NotImplementedException();
}
