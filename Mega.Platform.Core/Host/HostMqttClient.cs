using Mega.Platform.Core.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using System.Text.Json;

namespace Mega.Platform.Core.Host;

internal class HostMqttClient : IHostedService
{
    private readonly IMqttClient _mqttClient;
    private readonly IMqttMessageDispatcher _agentMqttMessageDispatcher;
    private volatile bool _isConnected = false;
    private readonly MqttServerConnectionOptions _mqttServerOptions;
    private readonly ILogger<HostMqttClient> _logger;

    public HostMqttClient(IMqttClient mqttClient,
        IOptions<MqttServerConnectionOptions> options,
        IMqttMessageDispatcher agentMqttMessageDispatcher,
        ILoggerFactory loggerFactory)
    {
        _mqttClient = mqttClient;
        _agentMqttMessageDispatcher = agentMqttMessageDispatcher;
        _mqttServerOptions = options.Value;
        _logger = loggerFactory.CreateLogger<HostMqttClient>();
        _mqttClient.DisconnectedAsync += MqttClient_DisconnectedAsync;
        _mqttClient.ApplicationMessageReceivedAsync += _mqttClient_ApplicationMessageReceivedAsync;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ConnectMqtt(cancellationToken);
        await _agentMqttMessageDispatcher.Start();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            if (_isConnected)
            {
                _mqttClient.DisconnectAsync();
                _mqttClient.Dispose();
            }
        });
    }

    private async Task _mqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        var response = await _agentMqttMessageDispatcher.SelectHandler(arg.ApplicationMessage.Topic).Handle(arg);
        if (response == null) response = new { Code = "FAIL", Message = "UNHANDLED ERROR" };
        await _mqttClient.PublishBinaryAsync(arg.ApplicationMessage.ResponseTopic,
             JsonSerializer.Serialize(response).GetBytes(),
             MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
             false);
    }

    private async Task MqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        _isConnected = false;

        while (!_isConnected)
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(5));
            _logger.LogInformation($"MqttClient is reconnecting...");

            await ConnectMqtt(cancellationTokenSource.Token);
        }
    }

    private async Task ConnectMqtt(CancellationToken cancellationToken)
    {
        try
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(_mqttServerOptions.KeepAlivePeriod);
            cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);
            _logger.LogInformation($"HostMqttClient is connecting to {_mqttServerOptions.Host}:{_mqttServerOptions.Port}");

            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_mqttServerOptions.Host, _mqttServerOptions.Port)
                .WithKeepAlivePeriod(_mqttServerOptions.KeepAlivePeriod)
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
                .Build();

            var result = await _mqttClient.ConnectAsync(mqttClientOptions, cancellationTokenSource.Token);

            if (result.ResultCode == MqttClientConnectResultCode.Success)
            {
                _isConnected = true;
                _logger.LogInformation($"HostMqttClient has connected to {_mqttServerOptions.Host}:{_mqttServerOptions.Port} succeed!");
                return;
            }

            _isConnected = false;
            _logger.LogError("Mqtt connection exception");
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _logger.LogError(ex, ex.Message);
        }
    }
}
