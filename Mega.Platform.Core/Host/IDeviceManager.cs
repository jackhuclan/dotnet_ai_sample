using Mega.Platform.Core.Client;

namespace Mega.Platform.Core.Host;

public interface IDeviceManager
{
    void AddDevice(DeviceDescriptor deviceDescriptor);
    void RemoveDevice(string deviceId);
    bool TryGetDevice(string deviceId, out DeviceDescriptor deviceDescriptor);
}
