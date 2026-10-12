using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Mega.Platform.Core.Models;

public class DeviceServiceInvokeResponse
{
    public static DeviceServiceInvokeResponse SUCCESS = new DeviceServiceInvokeResponse
    {
        Code = ErrorCodes.Sys.SUCCESS,
        Message = string.Empty,
        Params = new Dictionary<string, object?> { }
    };

    public static DeviceServiceInvokeResponse FAIL = new DeviceServiceInvokeResponse
    {
        Code = ErrorCodes.Sys.FAIL,
        Message = string.Empty,
        Params = new Dictionary<string, object?> { }
    };

    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object?> Params { get; set; } = new Dictionary<string, object?>();

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        });
    }
}
