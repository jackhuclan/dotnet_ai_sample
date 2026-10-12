namespace Mega.Platform.Core.Client.Command;

public class CommandDescriptor
{
    public CommandDescriptor(string commandId,
        string commandPath,
        CommandUsageScope executionScope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandPath);

        CommandId = commandId;
        CommandPath = commandPath;
        UsageScope = executionScope;
    }

    /// <summary>
    /// 指令唯一名称，设备唯一，不同设备可以相同
    /// </summary>
    public string CommandId { get; } = string.Empty;

    /// <summary>
    /// 指令挂载的路径: a/b/c
    /// </summary>
    public string CommandPath { get; } = string.Empty;

    /// <summary>
    /// 指令应用范围
    /// </summary>
    public CommandUsageScope UsageScope { get; } = CommandUsageScope.Both;

    public override int GetHashCode() => this.CommandPath.GetHashCode();
}
