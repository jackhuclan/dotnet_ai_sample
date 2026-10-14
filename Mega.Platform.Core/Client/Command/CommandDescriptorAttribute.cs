namespace Mega.Platform.Core.Client.Command;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class CommandDescriptorAttribute : Attribute
{
    public CommandDescriptorAttribute(string commandId,
        string commandPath,
        string description,
        CommandUsageScope executionScope = CommandUsageScope.Both)
    {
        Descriptor = new CommandDescriptor(commandId, commandPath, description, executionScope);
    }

    public CommandDescriptor Descriptor { get; }
}
