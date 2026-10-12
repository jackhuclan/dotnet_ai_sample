namespace Mega.Platform.Core.Client.Command;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class CommandDescriptorAttribute : Attribute
{
    public CommandDescriptorAttribute(string commandId,
        string commandPath,
        CommandUsageScope executionScope)
    {
        Descriptor = new CommandDescriptor(commandId, commandPath, executionScope);
    }

    public CommandDescriptor Descriptor { get; }
}
