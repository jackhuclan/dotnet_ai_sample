namespace Mega.Platform.Core.Client.Command
{
    public interface ICommandManager
    {
        Task AddCommand(DeviceDescriptor deviceDescriptor, IRemoteCommand remoteCommand);
        bool TryGetCommand(string commandPath, out IRemoteCommand? command);
    }
}