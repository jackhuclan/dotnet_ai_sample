using Microsoft.Extensions.AI;

namespace Mega.Platform.Core.Host.Command;

public interface IMqttFunctionTools
{
    AITool[] AsTools();
}
