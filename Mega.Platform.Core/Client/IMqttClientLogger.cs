using MQTTnet.Diagnostics.Logger;

namespace Mega.Platform.Core.Client;

public interface IMqttClientLogger
{
    void OutputMqttLog(MqttNetLogMessagePublishedEventArgs e);
}
