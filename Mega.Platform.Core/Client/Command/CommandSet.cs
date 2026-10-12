namespace Mega.Platform.Core.Client.Command;

/// <summary>
/// 指令集
/// </summary>
public class CommandSet : Dictionary<string, IRemoteCommand>
{
    /// <summary>
    /// add a remote command to collection. if CommandPath exists, the old command with same path will be replaced!
    /// </summary>
    /// <param name="remoteCommand"></param>
    public void AddCommand(IRemoteCommand remoteCommand)
    {
        this[remoteCommand.Descriptor.CommandPath] = remoteCommand;
    }

    public bool TryGetCommand(string commandPath, out IRemoteCommand? command)
    {
        if (!string.IsNullOrWhiteSpace(commandPath)
            && ContainsKey(commandPath))
        {
            command = this[commandPath];
            return true;
        }

        command = null;
        return false;
    }
}
