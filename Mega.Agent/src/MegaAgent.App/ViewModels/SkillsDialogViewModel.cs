using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MegaAgent.Core.Abstractions;
using MegaAgent.Core.Models;
using Microsoft.Extensions.Logging;

namespace MegaAgent.App.ViewModels;

/// <summary>
/// 「技能管理」对话框 ViewModel：技能列表、选中项与新建/编辑/删除/关闭命令。
/// 打开编辑器、弹确认框这类窗口交互通过 <see cref="IDialogService"/> 抽象交给 View，
/// 持久化与列表增删逻辑全部留在这里。依赖均由 DI 容器注入。
/// </summary>
public partial class SkillsDialogViewModel : ObservableObject
{
    private readonly ISkillStore _store;
    private readonly IDialogService _dialogs;
    private readonly ILogger<SkillsDialogViewModel> _logger;

    public ObservableCollection<Skill> Skills { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private Skill? _selectedSkill;

    /// <summary>当前是否选中了技能（编辑/删除按钮可用性）。</summary>
    public bool HasSelection => SelectedSkill is not null;

    /// <summary>请求 View 关闭对话框。</summary>
    public event EventHandler? CloseRequested;

    public SkillsDialogViewModel(ISkillStore store, IDialogService dialogs, ILogger<SkillsDialogViewModel> logger)
    {
        _store = store;
        _dialogs = dialogs;
        _logger = logger;
        foreach (var s in store.Skills)
            Skills.Add(s);
        SelectedSkill = Skills.FirstOrDefault();

        _logger.LogDebug("技能管理面板打开：共 {Count} 个技能", Skills.Count);
    }

    [RelayCommand]
    private void Add()
    {
        var skill = new Skill { Name = "新技能", Description = string.Empty, Prompt = string.Empty };
        if (!_dialogs.ShowSkillEditor(skill)) return;

        Skills.Add(skill);
        Persist();
        SelectedSkill = skill;
        _logger.LogInformation("新增技能 {SkillId}「{Name}」", skill.Id, skill.Name);
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
        _logger.LogInformation("更新技能 {SkillId}「{Name}」", selected.Id, selected.Name);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Delete()
    {
        if (SelectedSkill is not { } selected) return;
        if (!_dialogs.Confirm($"确定删除技能「{selected.Name}」吗？", "确认删除")) return;

        Skills.Remove(selected);
        SelectedSkill = Skills.FirstOrDefault();
        Persist();
        _logger.LogInformation("删除技能 {SkillId}「{Name}」", selected.Id, selected.Name);
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private void Persist()
    {
        _store.Skills = Skills.ToList();
        try
        {
            _store.Save();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "技能库保存失败，本次改动仅存在于内存");
        }
    }
}
