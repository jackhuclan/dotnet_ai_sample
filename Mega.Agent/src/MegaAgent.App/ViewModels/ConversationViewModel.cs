using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MegaAgent.Core.Models;

namespace MegaAgent.App.ViewModels;

/// <summary>一个对话会话的 UI 包装。</summary>
public partial class ConversationViewModel : ObservableObject
{
    public Conversation Model { get; }

    public ObservableCollection<MessageViewModel> Messages { get; } = new();

    /// <summary>会话标题（发送第一条消息后自动取用户输入）。</summary>
    [ObservableProperty]
    private string _title;

    /// <summary>当前会话使用的模型 id。</summary>
    [ObservableProperty]
    private string _modelId;

    /// <summary>当前会话关联的技能 id（null=不使用技能）。</summary>
    [ObservableProperty]
    private string? _skillId;

    /// <summary>是否处于「重命名」状态：列表项以文本框呈现。</summary>
    [ObservableProperty]
    private bool _isRenaming;

    /// <summary>重命名时的编辑缓冲区（未确认前不改动 Title）。</summary>
    [ObservableProperty]
    private string _renameDraft = string.Empty;

    /// <summary>
    /// 列表副标题显示的日期：今天 / 昨天显示相对时间（含时分），同年显示「M月d日」，
    /// 跨年显示「yyyy年M月d日」。取自会话创建时间（CreatedAt）。
    /// </summary>
    public string DisplayDate => FormatDate(Model.CreatedAt);

    /// <summary>完整时间，供列表项悬浮提示使用。</summary>
    public string FullDate => Model.CreatedAt.ToString("yyyy-MM-dd HH:mm");

    private static string FormatDate(DateTime time)
    {
        var today = DateTime.Today;
        if (time.Date == today)
            return $"今天 {time:HH:mm}";
        if (time.Date == today.AddDays(-1))
            return $"昨天 {time:HH:mm}";
        if (time.Year == today.Year)
            return $"{time.Month}月{time.Day}日";
        return $"{time.Year}年{time.Month}月{time.Day}日";
    }

    public ConversationViewModel(Conversation model)
    {
        Model = model;
        _title = model.Title;
        _modelId = model.ModelId;
        _skillId = model.SkillId;
        foreach (var m in model.Messages)
            Messages.Add(new MessageViewModel(m));
    }

    /// <summary>把 UI 状态回写到持久化模型。</summary>
    public void SyncToModel()
    {
        Model.Title = Title;
        Model.ModelId = ModelId;
        Model.SkillId = SkillId;
        Model.Messages = Messages.Select(m => m.ToModel()).ToList();
    }
}
