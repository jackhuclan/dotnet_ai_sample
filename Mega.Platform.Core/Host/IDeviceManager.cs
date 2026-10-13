using Mega.Platform.Core.Client;

namespace Mega.Platform.Core.Host;

public interface IDeviceManager
{
    IReadOnlyList<string> DeviceIds { get; }
    void AddDevice(DeviceDescriptor deviceDescriptor);
    void RemoveDevice(string deviceId);
    bool TryGetDevice(string deviceId, out DeviceDescriptor deviceDescriptor);
}
