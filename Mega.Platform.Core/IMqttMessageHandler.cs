using MQTTnet;

namespace Mega.Platform.Core;

internal interface IMqttMessageHandler
{
    Task<object> Handle(MqttApplicationMessageReceivedEventArgs applicationMessageArgs);
}
