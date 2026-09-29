using Microsoft.Agents.AI;

namespace dotnet_ai_sample;

internal class MySkill : AgentClassSkill<MySkill>
{
    public override AgentSkillFrontmatter Frontmatter => throw new NotImplementedException();

    protected override string Instructions => throw new NotImplementedException();

    public override IReadOnlyList<AgentSkillScript>? Scripts => base.Scripts;
}
