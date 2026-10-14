using Mega.Platform.Core.Client;
using Xunit;

namespace Mega.Platform.Core.UnitTests;

public sealed class DeviceDescriptorTest
{
    [Fact]
    public void TestMethod1()
    {
        var d1 = new DeviceDescriptor()
        {
            ProductId = "product1",
            DeviceId = "device1",
            DeviceName = "Device 1"
        };
        var d2 = new DeviceDescriptor()
        {
            ProductId = "product1",
            DeviceId = "device1",
            DeviceName = "Device 1"
        };

        Assert.Equal(d1, d2);
        Assert.True(d1 == d2);
    }
}
