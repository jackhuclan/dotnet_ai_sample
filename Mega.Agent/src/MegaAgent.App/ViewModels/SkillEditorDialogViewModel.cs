using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MegaAgent.Core.Models;
using Microsoft.Extensions.Logging;

namespace MegaAgent.App.ViewModels;

/// <summary>
/// 「编辑技能」对话框 ViewModel：技能草稿字段、实时必填校验与保存/取消命令。
/// 保存成功后直接写回传入的 <see cref="Skill"/> 实例，由调用方负责持久化。
/// 运行期参数 <see cref="Skill"/> 由 ActivatorUtilities 传入，日志器由容器注入。
/// </summary>
public partial class SkillEditorDialogViewModel : ObservableObject
{
    private readonly Skill _skill;
    private readonly ILogger<SkillEditorDialogViewModel> _logger;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NameHasError))]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DescriptionHasError))]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _description;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PromptHasError))]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _prompt;

    /// <summary>技能名称为空 → 必填未满足。</summary>
    public bool NameHasError => string.IsNullOrWhiteSpace(Name);

    /// <summary>技能描述为空 → 必填未满足。</summary>
    public bool DescriptionHasError => string.IsNullOrWhiteSpace(Description);

    /// <summary>系统提示词为空 → 必填未满足。</summary>
    public bool PromptHasError => string.IsNullOrWhiteSpace(Prompt);

    /// <summary>三个必填项都已填写。</summary>
    public bool CanSubmit => !NameHasError && !DescriptionHasError && !PromptHasError;

    /// <summary>请求 View 关闭对话框，参数为 DialogResult（null 表示仅关闭）。</summary>
    public event EventHandler<bool?>? CloseRequested;

    public SkillEditorDialogViewModel(Skill skill, ILogger<SkillEditorDialogViewModel> logger)
    {
        _skill = skill;
        _logger = logger;
        _name = skill.Name ?? string.Empty;
        _description = skill.Description ?? string.Empty;
        _prompt = skill.Prompt ?? string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private void Save()
    {
        _skill.Name = Name.Trim();
        _skill.Description = Description.Trim();
        _skill.Prompt = Prompt.Trim();
        _logger.LogInformation("技能草稿已提交保存：{SkillId}「{Name}」（{PromptLength} 字提示词）",
            _skill.Id, _skill.Name, _skill.Prompt.Length);
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
