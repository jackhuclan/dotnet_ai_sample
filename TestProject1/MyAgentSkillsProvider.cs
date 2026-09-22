using Microsoft.Agents.AI;

namespace dotnet_ai_agent_sample
{
    internal class MyAgentSkillsProvider : AIContextProvider
    {
        public MyAgentSkillsProvider()
        {
        }

        protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
        {
            return base.ProvideAIContextAsync(context, cancellationToken);
        }
    }
}
