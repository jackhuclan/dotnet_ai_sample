using Mega.Platform.Core.Host;
using Mega.Platform.Web.Models;
using MQTTnet;
using System.Text.Json;

namespace Mega.Platform.Core.Host.Handler;

internal class StatusMqttMessageHandler : IMqttMessageHandler
{
    public async Task<object> Handle(MqttApplicationMessageReceivedEventArgs args)
    {
        var deviceStatusReportRequest = JsonSerializer.Deserialize<DeviceStatusReportRequest>(args.ApplicationMessage.PayloadSegment);
        var response = new DeviceStatusReportResponse
        {
            Code = "OK",
            Message = DateTime.UtcNow.ToString("o")
        };
        args.ApplicationMessage.PayloadSegment = JsonSerializer.Serialize(response).GetBytes();
        return response;
    }
}
