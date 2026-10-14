using Mega.Platform.Core.Models;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Mega.Platform.Core.Host;

public class AIToolManager : IAIToolManager
{
    private readonly ConcurrentDictionary<string, AITool> _tools = new();
    private readonly IDeviceServiceInvoker _deviceServiceInvoker;

    public IList<AITool> Tools => _tools.Values.ToList();

    public AIToolManager(IDeviceServiceInvoker deviceServiceInvoker)
    {
        _deviceServiceInvoker = deviceServiceInvoker;
    }

    public void RegisterTool(DeviceCommandReportRequest commandRequest)
    {
        if (!_tools.TryGetValue(commandRequest.CommandId, out var function))
        {
            var invokerParam = Expression.Parameter(typeof(IDeviceServiceInvoker), "invoker");
            var reqParam = Expression.Parameter(typeof(object), "req");
            var invokeMethod = typeof(IDeviceServiceInvoker).GetMethod("InvokeService", [typeof(DeviceServiceInvokeRequest)]);
            Throw.IfNull(invokeMethod, nameof(invokeMethod));

            var callExpr = Expression.Call(invokerParam, invokeMethod, reqParam);
            var template = Expression.Lambda<Func<IDeviceServiceInvoker, DeviceServiceInvokeRequest, Task<DeviceServiceInvokeResponse>>>(callExpr, invokerParam, reqParam);

            CommandFunction func = req => template.Compile()(_deviceServiceInvoker, req);

            function = AIFunctionFactory.Create(func, commandRequest.CommandId, commandRequest.Description);
            _tools[commandRequest.CommandId] = function;
        }
    }
}
