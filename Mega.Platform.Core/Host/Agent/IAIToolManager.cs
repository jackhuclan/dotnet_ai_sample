using Mega.Platform.Core.Models;
using Microsoft.Extensions.AI;

namespace Mega.Platform.Core.Host.Agent;

public interface IAIToolManager
{
    IList<AITool> Tools { get; }

    void RegisterTool(DeviceCommandReportRequest commandRequest);
}