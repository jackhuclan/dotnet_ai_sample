using Mega.Platform.Core.Client.Command;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;
using MQTTnet;
using MQTTnet.Protocol;

namespace Mega.Platform.Core.Client;

public class DefaultDevice : IDevice
{
    public string ProductId { get; }
    public string DeviceId { get; }
    public string DeviceName { get; }
    public string ClientId { get; }
    public IObjectFactory ObjectFactory { get; private set; }
    public IMqttClientWrapper MqttClientWrapper { get; }
    public ICommandManager CommandManager { get; }
    /// <summary>
    /// 全局服务
    /// </summary>
    public IServiceProvider ApplicationServices { get; }
    /// <summary>
    /// 设备描述符
    /// </summary>
    public DeviceDescriptor DeviceDescriptor { get; private set; }
    private readonly IMqttClient _mqttClient;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<DefaultDevice> _logger;

    protected DefaultDevice(IOptions<DeviceDescriptor> deviceDescriptorOptions,
        IServiceProvider serviceProvider)
    {
        ApplicationServices = serviceProvider;
        DeviceDescriptor = deviceDescriptorOptions.Value;
        ProductId = DeviceDescriptor.ProductId;
        DeviceId = DeviceDescriptor.DeviceId;
        DeviceName = DeviceDescriptor.DeviceName;
        ClientId = DeviceDescriptor.DeviceId;

        _applicationLifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
        MqttClientWrapper = serviceProvider.GetRequiredService<IMqttClientWrapper>();
        ObjectFactory = serviceProvider.GetRequiredService<IObjectFactory>();
        _loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = _loggerFactory.CreateLogger<DefaultDevice>();
        _mqttClient = serviceProvider.GetRequiredService<IMqttClient>();
        CommandManager = serviceProvider.GetRequiredService<ICommandManager>();
        MqttClientWrapper.OnConnected += async () => await OnMqttConnected();
        MqttClientWrapper.OnDisconnected += async () => await OnMqttDisconnected();
        _applicationLifetime.ApplicationStarted.Register(async () => await OnApplicationStarted());
        _applicationLifetime.ApplicationStopping.Register(async () => await OnApplicationStopping());
    }

    /// <summary>
    /// 添加一条指令
    /// </summary>
    /// <param name="remoteCommand">指令</param>
    public void AddCommand(IRemoteCommand remoteCommand)
    {
        CommandManager.AddCommand(remoteCommand);
    }

    /// <summary>
    /// 添加一条指令
    /// </summary>
    /// <param name="commandDescriptor">指令描述，包含commandPath,即服务的全路径地址</param>
    /// <param name="command">指令动作</param>
    /// <param name="commandUsage">指令用途种类</param>
    public void AddCommand(CommandDescriptor commandDescriptor, CommandFunction command, CommandUsageScope commandUsage = CommandUsageScope.Both)
    {
        Throw.IfNullOrEmpty(commandDescriptor.CommandId);
        Throw.IfNullOrEmpty(commandDescriptor.CommandPath);
        Throw.IfNull(command);

        CommandManager.AddCommand(ObjectFactory.CreateObject<DelegateCommand<DefaultDevice>>(ApplicationServices, this, commandDescriptor, command));
    }

    /// <summary>
    /// do something after application started
    /// </summary>
    protected virtual async Task OnApplicationStarted()
    {
        _ = MqttClientWrapper.ConnectMqtt(this, _applicationLifetime.ApplicationStarted);
    }

    protected virtual async Task OnMqttConnected()
    {
        var servicePath = MqttTopics.GetSubscribeTopic(MqttTopics.ServiceTopicTemplate, DeviceDescriptor.ProductId, DeviceDescriptor.DeviceId);
        var subscribeResult = await _mqttClient.SubscribeAsync(servicePath, MqttQualityOfServiceLevel.AtLeastOnce);
        _logger.LogInformation($"SubscribeService: {servicePath},ResultCode={subscribeResult.Items.FirstOrDefault()?.ResultCode}");
    }

    protected virtual async Task OnMqttDisconnected() => await Task.CompletedTask;

    /// <summary>
    /// do something when application is closing
    /// </summary>
    protected virtual async Task OnApplicationStopping() => await Task.CompletedTask;
}
