using MQTTnet;

namespace Mega.Platform.Core.Handler;

internal class AlarmMqttMessageHandler : IMqttMessageHandler
{
    public AlarmMqttMessageHandler() { }
    public Task<object> Handle(MqttApplicationMessageReceivedEventArgs applicationMessageArgs) => throw new NotImplementedException();
}
