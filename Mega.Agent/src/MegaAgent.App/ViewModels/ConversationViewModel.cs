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
