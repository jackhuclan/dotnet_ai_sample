namespace Mega.Platform.Core.Client.Command;

/// <summary>
/// 指令用途种类
/// </summary>
[Flags]
public enum CommandUsageScope
{
    /// <summary>
    /// 默认本机调用
    /// </summary>
    Local = 0,

    /// <summary>
    /// 远程下发调用
    /// </summary>
    Mqtt = 1,

    /// <summary>
    /// 管道调用
    /// </summary>
    Pipe = 2,

    /// <summary>
    /// mqtt & pipe
    /// </summary>
    Both = Mqtt | Pipe,
}
