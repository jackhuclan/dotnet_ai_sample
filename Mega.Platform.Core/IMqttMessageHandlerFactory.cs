using Mega.Platform.Core.Host;

namespace Mega.Platform.Core;

internal interface IMqttMessageHandlerFactory
{
    IMqttMessageHandler CreateHandler(string topic);
}
