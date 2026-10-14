namespace Mega.Platform.Core.Client;

public interface IMqttClientWrapper : IDisposable
{
    bool IsConnected { get; }
    Task ConnectMqtt(IDevice device, CancellationToken cancellationToken = default);
    Task<bool> Report(IDevice device, bool reported);

    event Func<Task> OnConnected;
    event Func<Task> OnDisconnected;
}
