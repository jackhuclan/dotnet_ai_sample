using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mega.Platform.Core.Client;

internal class MqttClientLogger : IMqttClientLogger
{
    private readonly MqttNetEventLogger _mqttNetLogger;
    private readonly ILogger<MqttClientLogger> _logger;
    private readonly MqttServerConnectionOptions _serverOptions;

    public MqttClientLogger(MqttNetEventLogger mqttNetLogger,
        IOptions<MqttServerConnectionOptions> options,
        ILogger<MqttClientLogger> logger)
    {
        if (mqttNetLogger == null) throw new ArgumentNullException(nameof(mqttNetLogger));
        _mqttNetLogger = mqttNetLogger;
        _logger = logger;
        _serverOptions = options.Value;
        _mqttNetLogger.LogMessagePublished -= OutputMqttLog;
        _mqttNetLogger.LogMessagePublished += OutputMqttLog;
    }

    public void OutputMqttLog(MqttNetLogMessagePublishedEventArgs e)
    {
        switch (e.LogMessage.Level)
        {
            case MqttNetLogLevel.Error:
                _logger.LogError(e.LogMessage.Message, e.LogMessage.Exception);
                break;
            case MqttNetLogLevel.Warning:
                _logger.LogWarning(e.LogMessage.Message);
                break;
            case MqttNetLogLevel.Verbose:
                _logger.LogDebug(e.LogMessage.Message);
                break;
            case MqttNetLogLevel.Info:
                _logger.LogInformation(e.LogMessage.Message);
                break;
        }
    }

    private void OutputMqttLog(object? sender, MqttNetLogMessagePublishedEventArgs e)
    {
        if (_serverOptions.EnableLogMqttClient)
        {
            OutputMqttLog(e);
        }
    }
}
