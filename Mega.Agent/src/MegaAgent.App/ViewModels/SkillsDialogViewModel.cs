using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MegaAgent.Core.Abstractions;
using MegaAgent.Core.Models;

namespace MegaAgent.App.ViewModels;

/// <summary>
/// 「技能管理」对话框 ViewModel：技能列表、选中项与新建/编辑/删除/关闭命令。
/// 打开编辑器、弹确认框这类窗口交互通过 <see cref="IDialogService"/> 抽象交给 View，
/// 持久化与列表增删逻辑全部留在这里。两个依赖均由 DI 容器注入。
/// </summary>
public partial class SkillsDialogViewModel : ObservableObject
{
    private readonly ISkillStore _store;
    private readonly IDialogService _dialogs;

    public ObservableCollection<Skill> Skills { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private Skill? _selectedSkill;

    /// <summary>当前是否选中了技能（编辑/删除按钮可用性）。</summary>
    public bool HasSelection => SelectedSkill is not null;

    /// <summary>请求 View 关闭对话框。</summary>
    public event EventHandler? CloseRequested;

    public SkillsDialogViewModel(ISkillStore store, IDialogService dialogs)
    {
        _store = store;
        _dialogs = dialogs;
        foreach (var s in store.Skills)
            Skills.Add(s);
        SelectedSkill = Skills.FirstOrDefault();
    }

    [RelayCommand]
    private void Add()
    {
        var skill = new Skill { Name = "新技能", Description = string.Empty, Prompt = string.Empty };
        if (!_dialogs.ShowSkillEditor(skill)) return;

        Skills.Add(skill);
        Persist();
        SelectedSkill = skill;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Edit()
    {
        if (SelectedSkill is not { } selected) return;
        if (!_dialogs.ShowSkillEditor(selected)) return;

        // Skill 不是可观察对象，替换同位置元素以强制列表刷新显示
        var index = Skills.IndexOf(selected);
        if (index >= 0) Skills[index] = selected;
        SelectedSkill = selected;
        Persist();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Delete()
    {
        if (SelectedSkill is not { } selected) return;
        if (!_dialogs.Confirm($"确定删除技能「{selected.Name}」吗？", "确认删除")) return;

        Skills.Remove(selected);
        SelectedSkill = Skills.FirstOrDefault();
        Persist();
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private void Persist()
    {
        _store.Skills = Skills.ToList();
        _store.Save();
    }
}
