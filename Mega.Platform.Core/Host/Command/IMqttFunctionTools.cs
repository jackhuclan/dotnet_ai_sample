using Microsoft.Extensions.AI;

namespace Mega.Platform.Core.Host.Command;

internal interface IMqttFunctionTools
{
    AITool[] AsTools();
}
