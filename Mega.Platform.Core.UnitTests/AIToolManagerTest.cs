using Mega.Platform.Core.Host.Agent;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mega.Platform.Core.UnitTests;

public class AIToolManagerTest
{
    [Fact]
    public void ToolShouldOnlyOneTime_WhenSameTool_FromSameDevcie()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IObjectFactory, ObjectFactory>();
        serviceCollection.AddSingleton<IAIToolManager, AIToolManager>();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var toolManager = serviceProvider.GetRequiredService<IAIToolManager>();
        string device1 = "d001";
        toolManager.RegisterTool(new Models.DeviceCommandRequest
        {
            DeviceId = device1,
            CommandId = "a_b_c",
            CommandPath = "a/b/c"
        });
        toolManager.RegisterTool(new Models.DeviceCommandRequest
        {
            DeviceId = device1,
            CommandId = "a_b_c",
            CommandPath = "a/b/c"
        });

        var tools_of_device1 = toolManager.Tools(device1);
        Assert.Single(tools_of_device1);
    }

    [Fact]
    public void EachDeviceHasSameNameTool_WhenSameTool_FromDifferentDevcie()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IObjectFactory, ObjectFactory>();
        serviceCollection.AddSingleton<IAIToolManager, AIToolManager>();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var toolManager = serviceProvider.GetRequiredService<IAIToolManager>();
        string device1 = "d001";
        string device2 = "d002";
        toolManager.RegisterTool(new Models.DeviceCommandRequest
        {
            DeviceId = device1,
            CommandId = "a_b_c",
            CommandPath = "a/b/c"
        });
        toolManager.RegisterTool(new Models.DeviceCommandRequest
        {
            DeviceId = device2,
            CommandId = "a_b_c",
            CommandPath = "a/b/c"
        });

        var tools_of_device1 = toolManager.Tools(device1);
        var tools_of_device2 = toolManager.Tools(device2);
        Assert.Single(tools_of_device1);
        Assert.Single(tools_of_device2);
    }
}
