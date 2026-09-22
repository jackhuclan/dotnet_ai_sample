using Microsoft.Extensions.AI;

namespace dotnet_ai_agent_sample;

internal class CustomTool : AIFunction
{
    protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<object?>("customtool");
    }
}
