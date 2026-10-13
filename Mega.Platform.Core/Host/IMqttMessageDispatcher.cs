namespace Mega.Platform.Core.Host;

internal interface IMqttMessageDispatcher
{
    IMqttMessageHandler this[string topic] { get; }
    IMqttMessageHandler? SelectHandler(string topic);
    Task Start();
}
