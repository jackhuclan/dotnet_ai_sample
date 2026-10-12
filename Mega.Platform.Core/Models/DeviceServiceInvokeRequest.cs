using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Mega.Platform.Core.Models;

public class DeviceServiceInvokeRequest
{
    private string _serviceName = string.Empty;

    /// <summary>
    /// source device's productId
    /// </summary>
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// source device's DeviceId
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// source device's ClientId
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// source device's host ip address & port, ex: http://192.168.1.100:8001
    /// </summary>
    public string HostAddress { get; set; } = string.Empty;

    /// <summary>
    /// source device's ServiceId
    /// </summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>
    /// 服务下面的服务指令：serviceId/serviceName
    /// </summary>
    public string ServiceName
    {
        get
        {
            if (string.IsNullOrEmpty(_serviceName))
                _serviceName = ServiceId;

            return _serviceName;
        }
        set => _serviceName = value;
    }

    public string? EventId { get; set; } = string.Empty;
    public string? EventName { get; set; } = string.Empty;

    public PanelList PayloadPanels { get; set; } = new();
    public CutterTrays PayloadCutterTrays { get; set; } = new();
    public DateTime CreateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// target device's productId
    /// </summary>
    public string? TargetProductId { get; set; }

    /// <summary>
    /// target device's DeviceId
    /// </summary>
    public string? TargetDeviceId { get; set; }

    /// <summary>
    /// target device's ClientId
    /// </summary>
    public string? TargetClientId { get; set; }

    /// <summary>
    /// target device's host ip address & port, ex: http://192.168.1.100:8001
    /// </summary>
    public string TargetHostAddress { get; set; } = string.Empty;

    /// <summary>
    /// target device's Interaction Behavior requested by agv
    /// </summary>
    public ushort CallerRequestInteractionBehavior { get; set; } = InteractionBehavior.Noop;

    /// <summary>
    /// agv上下料交互方向
    /// </summary>
    public InteractionPosition CallerRequestInteractionDirection { get; set; } = InteractionPosition.Rear;

    public ProductStatus CallerRequestInputProductStatus { get; set; } = ProductStatus.Noop;
    public ProductStatus CallerRequestOutputProductStatus { get; set; } = ProductStatus.Noop;
    public List<ProductStatus> CallerInputCapabilities { get; set; } = new List<ProductStatus>();
    public List<ProductStatus> CallerOutputCapabilities { get; set; } = new List<ProductStatus>();

    /// <summary>
    /// agv交互板料类型
    /// </summary>
    public MaterialKind CallerRequestMaterialKind { get; set; } = MaterialKind.Panel;

    /// <summary>
    /// 调度服务的状态
    /// </summary>
    public ScheduledTaskStatus ScheduledStatus { get; set; } = ScheduledTaskStatus.None;

    /// <summary>
    /// 服务调用类型,是否等待回复
    /// </summary>
    public ServiceInvocationKind InvocationKind { get; set; } = ServiceInvocationKind.RequestReply;

    /// <summary>
    /// 请求发出的库位编号
    /// </summary>
    public string LocationCode
    {
        get
        {
            return Params.ContainsKey("DeviceCode") ? Params["DeviceCode"].ToStr() : string.Empty;
        }
        set
        {
            Params["DeviceCode"] = value;
        }
    }

    public string TaskEventTraceId
    {
        get
        {
            return Params.ContainsKey("TaskEventTraceId") ? Params["TaskEventTraceId"].ToStr() : string.Empty;
        }
        set
        {
            Params["TaskEventTraceId"] = value;
        }
    }

    /// <summary>
    /// parameters for source device call target device
    /// </summary>
    public Dictionary<string, object?> Params { get; set; } = new Dictionary<string, object?>();

    public string RequestTopic { get; set; } = string.Empty;

    public string ReplyTopic { get; set; } = string.Empty;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        });
    }
}
