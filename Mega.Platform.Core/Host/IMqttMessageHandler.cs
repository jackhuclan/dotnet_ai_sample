using MQTTnet;

namespace Mega.Platform.Core.Host;

internal interface IMqttMessageHandler
{
    Task<object> Handle(MqttApplicationMessageReceivedEventArgs applicationMessageArgs);
}
