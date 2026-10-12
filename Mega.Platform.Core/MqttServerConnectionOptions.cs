namespace Mega.Platform.Core;

public class MqttServerConnectionOptions
{
    //
    // 摘要:
    //     mqtt Server服务器地址
    public string Host { get; set; } = string.Empty;

    //
    // 摘要:
    //     mqtt Server服务器端口
    public int Port { get; set; }

    //
    // 摘要:
    //     登录用户名
    public string UserName { get; set; } = string.Empty;

    //
    // 摘要:
    //     登录用户的密码
    public string Password { get; set; } = string.Empty;

    //
    // 摘要:
    //     登录客户端id
    public string ClientId { get; set; } = string.Empty;

    //
    // 摘要:
    //     是否启用tls，默认为false
    public bool UseTls { get; set; }

    //
    // 摘要:
    //     客户端证书路径
    public string CertPemFilePath { get; set; } = string.Empty;

    //
    // 摘要:
    //     mqtt client keep alive time
    public TimeSpan KeepAlivePeriod { get; set; } = TimeSpan.FromSeconds(60);

    //
    // 摘要:
    //     mqtt client heart beat time
    public TimeSpan HeartbeatPeriod { get; set; } = TimeSpan.FromSeconds(2);

    //
    // 摘要:
    //     mqtt client 连接超时时间
    public TimeSpan MqttConnectionTimeOut { get; set; } = TimeSpan.FromSeconds(15);

    //
    // 摘要:
    //     是否启用心跳，默认false
    public bool EnableHeartbeat { get; set; }

    //
    // 摘要:
    //     设备端，是否开启mqtt日志订阅，默认不订阅
    public bool EnableLogMqttClient { get; set; }
}
