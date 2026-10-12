namespace Mega.Platform.Core.Client.Command;

/// <summary>
/// 指令集
/// </summary>
public class CommandSet : Dictionary<string, IRemoteCommand>
{
    /// <summary>
    /// add a remotecommand to collection. if CommandName exists, the old command with same name will be replaced!
    /// </summary>
    /// <param name="remoteCommand"></param>
    public void AddCommand(IRemoteCommand remoteCommand)
    {
        this[remoteCommand.Descriptor.CommandId] = remoteCommand;
    }

    public bool TryGetCommand(string commandName, out IRemoteCommand? command)
    {
        if (!string.IsNullOrWhiteSpace(commandName)
            && ContainsKey(commandName))
        {
            command = this[commandName];
            return true;
        }

        command = null;
        return false;
    }
}
