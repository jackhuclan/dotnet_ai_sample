using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using System.Security.Cryptography.X509Certificates;

namespace Mega.Platform.Core.Client;

public class MqttClientWrapper : IMqttClientWrapper
{
    private volatile bool _isConnected = false;
    private readonly IMqttClient _mqttClient;
    private readonly IClientMqttApplicationMessageListener _clientMqttApplicationMessageListener;
    private readonly IMqttClientLogger _mqttClientLogger;
    private readonly ILogger<MqttClientWrapper> _logger;
    private readonly MqttServerConnectionOptions _mqttServerOptions;
    private readonly PeriodicTimer _periodicTimer;
    private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(true);
    private volatile bool _disposing = false;

    public event Func<Task> OnConnected;

    public event Func<Task> OnDisconnected;

    /// <summary>
    /// mqtt是否连接
    /// </summary>
    public bool IsConnected
    {
        get { return _isConnected; }
        private set
        {
            _isConnected = value;

            if (_isConnected)
                OnConnected.Invoke();
            else
                OnDisconnected.Invoke();
        }
    }

    public MqttClientWrapper(IMqttClient mqttClient,
        IOptions<MqttServerConnectionOptions> options,
        ILoggerFactory loggerFactory,
        IClientMqttApplicationMessageListener clientMqttApplicationMessageListner,
        IMqttClientLogger mqttClientLogger)
    {
        _mqttClient = mqttClient;
        _clientMqttApplicationMessageListener = clientMqttApplicationMessageListner;
        _mqttClientLogger = mqttClientLogger;
        _mqttServerOptions = options.Value;
        mqttClient.ApplicationMessageReceivedAsync += arg => _clientMqttApplicationMessageListener.OnApplicationMessageReceivedAsync(arg);
        mqttClient.DisconnectedAsync += MqttClient_DisconnectedAsync;
        _periodicTimer = new PeriodicTimer(_mqttServerOptions.HeartbeatPeriod);
        _logger = loggerFactory.CreateLogger<MqttClientWrapper>();

        OnConnected += () => Task.CompletedTask;
        OnDisconnected += () => Task.CompletedTask;
    }

    public async Task ConnectMqtt(IDevice device, CancellationToken cancellationToken = default)
    {
        while (await _periodicTimer.WaitForNextTickAsync())
        {
            _logger.LogInformation($"MqttClientWrapper check mqtt...");

            if (!_isConnected)
            {
                _logger.LogInformation($"mqtt is broken, MqttClientWrapper ConnectMqtt again...");
                await DoConnectMqttInternal();
            }
            else if (_mqttServerOptions.EnableHeartbeat && IsConnected)
            {
                var publishSuccess = await _clientMqttApplicationMessageListener.OnHeartbeat(device);
                if (!publishSuccess)
                {
                    await _mqttClient.DisconnectAsync(MqttClientDisconnectOptionsReason.UnspecifiedError, "heartbeat packet send failed");
                    break;
                }
            }
        }
    }

    private Task MqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        _logger.LogInformation($"MqttClient disconnected with error={arg.ReasonString}!");
        IsConnected = false;

        return Task.CompletedTask;
    }

    private async Task DoConnectMqttInternal()
    {
        try
        {
            _autoResetEvent.WaitOne();
            if (_isConnected)
            {
                return;
            }

            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(_mqttServerOptions.MqttConnectionTimeOut);
                _logger.LogInformation($"MqttClientWrapper is connecting to {_mqttServerOptions.Host}:{_mqttServerOptions.Port}");

                string certPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _mqttServerOptions.CertPemFilePath);
                var mqttClientBuilder = new MqttClientOptionsBuilder();
                if (_mqttServerOptions.UseTls
                    && File.Exists(certPath))
                {
                    var x509Certificate2s = new X509Certificate2Collection();
                    x509Certificate2s.ImportFromPemFile(certPath);
                    mqttClientBuilder = mqttClientBuilder.WithTlsOptions(o => o.WithClientCertificates(x509Certificate2s));
                }

                if (!string.IsNullOrWhiteSpace(_mqttServerOptions.ClientId))
                {
                    mqttClientBuilder.WithClientId(_mqttServerOptions.ClientId);
                }

                if (!string.IsNullOrWhiteSpace(_mqttServerOptions.UserName)
                    && !string.IsNullOrWhiteSpace(_mqttServerOptions.Password))
                {
                    mqttClientBuilder.WithCredentials(_mqttServerOptions.UserName, _mqttServerOptions.Password);
                }

                MqttClientOptions mqttClientOptions = mqttClientBuilder
                    .WithTcpServer(_mqttServerOptions.Host, _mqttServerOptions.Port)
                    .WithKeepAlivePeriod(_mqttServerOptions.KeepAlivePeriod)
                    .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
                    .Build();

                MqttClientConnectResult result = await _mqttClient.ConnectAsync(mqttClientOptions, cancellationTokenSource.Token);

                if (result.ResultCode == MqttClientConnectResultCode.Success)
                {
                    IsConnected = true;
                    _logger.LogInformation($"MqttClientWrapper has connected to {_mqttServerOptions.Host}:{_mqttServerOptions.Port} succeed!");
                    return;
                }

                IsConnected = false;
                _logger.LogError("Mqtt connection exception");
            }
            catch (Exception ex)
            {
                IsConnected = false;
                _logger.LogError(ex, ex.Message);
            }
        }
        finally
        {
            _autoResetEvent.Set();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposing)
        {
            return;
        }

        _disposing = true;
        _logger.LogInformation($"MqttClientWrapper Disposed!");
        _mqttClient.DisconnectedAsync -= MqttClient_DisconnectedAsync;
        _periodicTimer.Dispose();
        _mqttClient.Dispose();
    }
}
