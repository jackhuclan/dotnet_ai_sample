namespace Mega.Platform.Core.Host.Agent;

//public class InjectContextMiddleware
//{
//    public async ValueTask<object?> InjectContextMiddleware(
//         AIAgent agent,
//         FunctionInvocationContext context,
//         Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
//         CancellationToken ct)
//    {
//        agent.
//        var runOpts = AIAgent.CurrentRunContext?.RunOptions;
//        if (runOpts?.AdditionalProperties is { } props)
//        {
//            // 自动把additional里的值注入工具Arguments
//            if (props.TryGetValue("tenantId", out var tenant))
//            {
//                context.Arguments["tenantId"] = tenant;
//            }
//        }
//        // 继续执行tool
//        return await next(context, ct);
//    }
//}
