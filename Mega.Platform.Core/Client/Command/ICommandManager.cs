namespace Mega.Platform.Core.Client.Command;

public interface ICommandManager
{
    Task AddCommand(IRemoteCommand remoteCommand);
    bool TryGetCommand(string commandPath, out IRemoteCommand? command);
}