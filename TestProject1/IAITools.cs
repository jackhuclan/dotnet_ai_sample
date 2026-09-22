using Microsoft.Extensions.AI;

namespace dotnet_ai_agent_sample;

internal interface IAITools
{
    IReadOnlyList<AIFunction> AsAITools();
}
