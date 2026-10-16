using CommunityToolkit.Mvvm.ComponentModel;

namespace MegaAgent.App.ViewModels;

/// <summary>技能下拉框的选项。Id 为 null 表示“不使用技能”。</summary>
public class SkillOption
{
    public string? Id { get; }
    public string DisplayName { get; }

    public SkillOption(string? id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }

    public override string ToString() => DisplayName;
}
