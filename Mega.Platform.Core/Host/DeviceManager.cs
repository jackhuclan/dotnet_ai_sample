using Mega.Platform.Core.Client;
using System.Collections.Concurrent;

namespace Mega.Platform.Core.Host;

internal class DeviceManager : IDeviceManager
{
    private readonly ConcurrentDictionary<string, DeviceDescriptor> _devices = new();

    public bool TryGetDevice(string deviceId, out DeviceDescriptor deviceDescriptor)
    {
        return _devices.TryGetValue(deviceId, out deviceDescriptor!);
    }

    public void AddDevice(DeviceDescriptor deviceDescriptor)
    {
        _devices[deviceDescriptor.DeviceId] = deviceDescriptor;
    }

    public void RemoveDevice(string deviceId)
    {
        _devices.TryRemove(deviceId, out _);
    }
}
