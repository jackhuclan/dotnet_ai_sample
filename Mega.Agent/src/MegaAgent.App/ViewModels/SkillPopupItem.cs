using CommunityToolkit.Mvvm.ComponentModel;
using MegaAgent.Core.Models;

namespace MegaAgent.App.ViewModels;

/// <summary>斜杠(/)技能选择浮层中的一项。</summary>
public partial class SkillPopupItem : ObservableObject
{
    public Skill Skill { get; }

    [ObservableProperty]
    private bool _isHighlighted;

    public SkillPopupItem(Skill skill)
    {
        Skill = skill;
    }

    public string Name => Skill.Name;
    public string Description => Skill.Description;
}
