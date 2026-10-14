namespace Mega.Platform.Core.Client.Command;

public class CommandDescriptor
{
    /// <summary>
    /// Creates a new instance of the CommandDescriptor class.
    /// </summary>
    /// <param name="commandId">指令唯一名称，设备唯一，不同设备可以相同，snake_case（蛇形命名法）风格，比如: make_order</param>
    /// <param name="commandPath">指令挂载的路径: a/b/c</param>
    /// <param name="description">指令描述，一定要描述什么情况下使用该指令，描述清楚指令的作用和使用场景</param>
    /// <param name="executionScope"></param>
    public CommandDescriptor(string commandId,
        string commandPath,
        string description,
        CommandUsageScope executionScope = CommandUsageScope.Both)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        CommandId = commandId;
        CommandPath = commandPath;
        Description = description;
        UsageScope = executionScope;
    }

    /// <summary>
    /// 指令唯一名称，设备唯一，不同设备可以相同，snake_case（蛇形命名法）风格，比如: make_order
    /// </summary>
    public string CommandId { get; } = string.Empty;

    /// <summary>
    /// 指令挂载的路径: a/b/c
    /// </summary>
    public string CommandPath { get; } = string.Empty;
    /// <summary>
    /// 指令描述，一定要描述什么情况下使用该指令，描述清楚指令的作用和使用场景
    /// </summary>
    public string Description { get; } = string.Empty;

    /// <summary>
    /// 指令应用范围
    /// </summary>
    public CommandUsageScope UsageScope { get; } = CommandUsageScope.Both;

    public override int GetHashCode() => this.CommandPath.GetHashCode();
}
