using Mega.Platform.Core.Models;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Mega.Platform.Core.Host.Agent;

public class AIToolManager : IAIToolManager
{
    private readonly ConcurrentDictionary<DeviceCommandRecord, AITool> _tools = new();
    private readonly IObjectFactory _objectFactory;

    public IList<AITool> Tools(string deviceId) => _tools.Where(x => string.Compare(x.Key.DeviceId, deviceId, true) == 0)
        .Select(x => x.Value)
        .ToList();

    public AIToolManager(IObjectFactory objectFactory)
    {
        _objectFactory = objectFactory;
    }

    public void RegisterTool(DeviceCommandRequest commandRequest)
    {
        var key = DeviceCommandRecord.From(commandRequest);
        if (!_tools.TryGetValue(key, out var function))
        {
            var invokerParam = Expression.Parameter(typeof(IDeviceServiceInvoker), "invoker");
            var reqParam = Expression.Parameter(typeof(DeviceServiceInvokeRequest), "req");
            var invokeMethod = typeof(IDeviceServiceInvoker).GetMethod(nameof(IDeviceServiceInvoker.InvokeService), [typeof(DeviceServiceInvokeRequest)]);
            Throw.IfNull(invokeMethod, nameof(invokeMethod));

            var callExpr = Expression.Call(invokerParam, invokeMethod, reqParam);
            var template = Expression.Lambda<Func<IDeviceServiceInvoker, DeviceServiceInvokeRequest, Task<DeviceServiceInvokeResponse>>>(callExpr, invokerParam, reqParam);

            CommandFunction func = req => template.Compile()(_objectFactory.CreateObject<IDeviceServiceInvoker>(commandRequest), req);

            function = AIFunctionFactory.Create(func, commandRequest.CommandId, commandRequest.Description);
            _tools[key] = function;
        }
    }
}
