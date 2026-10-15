using Mega.Platform.Core.Utils;
using MQTTnet;
using MQTTnet.Protocol;
using Xunit;

namespace Mega.Platform.Core.UnitTests;

public class MqttClientTest
{
    [Fact]
    public async Task TestMqttClientConnection()
    {
        // Arrange
        MqttClientOptions mqttClientOptions = new MqttClientOptionsBuilder()
             .WithTcpServer("127.0.0.1", 1883)
             .WithClientId("test-client")
             .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
             .Build();
        var mqttClient = new MqttClientFactory().CreateMqttClient();
        var connected = false;
        mqttClient.ConnectedAsync += (e) =>
        {
            connected = true;
            return Task.CompletedTask;
        };
        // Act
        mqttClient.DisconnectedAsync += e =>
        {
            connected = false;
            return Task.CompletedTask;
        };

        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            var msg = e.ApplicationMessage.Payload.ToStr();
            Assert.NotEmpty(msg);
            return Task.CompletedTask;
        };

        var mqttClientConnectResult = await mqttClient.ConnectAsync(mqttClientOptions, TestContext.Current.CancellationToken);
        Assert.Equal(MqttClientConnectResultCode.Success, mqttClientConnectResult.ResultCode);
        // Assert
        Assert.True(connected);

        var subscribeResult = await mqttClient.SubscribeAsync("mega/service/p/aoi/d/M001", MqttQualityOfServiceLevel.AtLeastOnce, TestContext.Current.CancellationToken);

        Assert.NotEmpty(subscribeResult.Items);
        await Task.Delay(20 * 1000); // Wait for a moment to ensure the subscription is processed
    }
}
