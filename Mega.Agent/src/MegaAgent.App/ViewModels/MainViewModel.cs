using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MegaAgent.Core.Abstractions;
using MegaAgent.Core.Models;

namespace MegaAgent.App.ViewModels;

/// <summary>主窗口 ViewModel：会话列表、斜杠技能选择、发送/停止、设置入口。</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;
    private readonly ISkillStore _skillStore;
    private readonly IConversationStore _conversationStore;
    private readonly IAgentService _agentService;
    private readonly IDialogService _dialogs;
    private CancellationTokenSource? _cts;

    public ObservableCollection<ConversationViewModel> Conversations { get; } = new();

    /// <summary>技能下拉/斜杠菜单的选项集合（含“不使用技能”）。</summary>
    public ObservableCollection<SkillOption> SkillOptions { get; } = new();

    public string[] AvailableModels => AppSettings.AvailableModels;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCurrentConversation))]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private ConversationViewModel? _currentConversation;

    [ObservableProperty]
    private SkillOption? _selectedSkillOption;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string _draftInput = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private bool _isBusy;

    // ---------- 斜杠技能浮层 ----------
    public ObservableCollection<SkillPopupItem> SkillPopupItems { get; } = new();

    [ObservableProperty]
    private bool _isSkillPopupOpen;

    [ObservableProperty]
    private int _skillPopupIndex = -1;

    public bool HasCurrentConversation => CurrentConversation is not null;

    /// <summary>当前会话是否已挂载技能（用于输入区 chip 显示）。</summary>
    public bool HasActiveSkill => SelectedSkillOption?.Id is not null;

    public string ActiveSkillName => SelectedSkillOption?.Id is null ? string.Empty : SelectedSkillOption.DisplayName;

    /// <summary>左下角用户区显示的 API Key 状态文案。</summary>
    [ObservableProperty]
    private string _apiKeyStatus = string.Empty;

    public MainViewModel(
        IDialogService dialogs,
        ISettingsStore settingsStore,
        ISkillStore skillStore,
        IConversationStore conversationStore,
        IAgentService agentService)
    {
        _dialogs = dialogs;
        _settingsStore = settingsStore;
        _skillStore = skillStore;
        _conversationStore = conversationStore;
        _agentService = agentService;

        // 历史会话的模型名若不在当前可选列表中，回落到默认模型
        var migrated = false;
        foreach (var c in _conversationStore.Conversations)
        {
            if (!AppSettings.AvailableModels.Contains(c.ModelId))
            {
                c.ModelId = _settingsStore.Current.DefaultModel;
                migrated = true;
            }
            Conversations.Add(new ConversationViewModel(c));
        }
        if (migrated)
            _conversationStore.Save();

        RebuildSkillOptions();
        RefreshApiKeyStatus();
        CurrentConversation = Conversations.FirstOrDefault();
    }

    public void RefreshApiKeyStatus()
    {
        ApiKeyStatus = _settingsStore.Current.IsConfigured
            ? "API Key 已配置"
            : "未配置 API Key";
    }

    partial void OnCurrentConversationChanged(ConversationViewModel? value)
    {
        if (value is not null)
            SelectedSkillOption = SkillOptions.FirstOrDefault(o => o.Id == value.SkillId) ?? SafeDefaultSkillOption();
        SendMessageCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSkillOptionChanged(SkillOption? value)
    {
        if (CurrentConversation is not null && value is not null)
        {
            CurrentConversation.SkillId = value.Id;
            SaveConversations();
        }
        OnPropertyChanged(nameof(HasActiveSkill));
        OnPropertyChanged(nameof(ActiveSkillName));
    }

    partial void OnDraftInputChanged(string value) => UpdateSkillPopup(value);

    private SkillOption? SafeDefaultSkillOption() => SkillOptions.FirstOrDefault();

    public void RebuildSkillOptions()
    {
        SkillOptions.Clear();
        SkillOptions.Add(new SkillOption(null, "不使用技能"));
        foreach (var s in _skillStore.Skills)
            SkillOptions.Add(new SkillOption(s.Id, s.Name));
        SelectedSkillOption = SkillOptions.FirstOrDefault()
                              ?? new SkillOption(null, "不使用技能");
        OnPropertyChanged(nameof(HasActiveSkill));
        OnPropertyChanged(nameof(ActiveSkillName));
    }

    // ---------------- 斜杠(/)技能选择 ----------------

    /// <summary>输入以 / 开头时打开技能浮层，并按已输入关键字过滤。</summary>
    private void UpdateSkillPopup(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.TrimStart().StartsWith('/'))
        {
            CloseSkillPopup();
            return;
        }

        var query = value.TrimStart()[1..].Trim();
        var matches = _skillStore.Skills
            .Where(s => query.Length == 0
                        || s.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || s.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            CloseSkillPopup();
            return;
        }

        SkillPopupItems.Clear();
        foreach (var s in matches)
            SkillPopupItems.Add(new SkillPopupItem(s));

        SetSkillPopupIndex(0);
        IsSkillPopupOpen = true;
    }

    private void SetSkillPopupIndex(int index)
    {
        if (SkillPopupItems.Count == 0) return;
        index = Math.Clamp(index, 0, SkillPopupItems.Count - 1);
        for (var i = 0; i < SkillPopupItems.Count; i++)
            SkillPopupItems[i].IsHighlighted = i == index;
        SkillPopupIndex = index;
    }

    /// <summary>上下键移动高亮项。</summary>
    public void MoveSkillSelection(int delta)
    {
        if (!IsSkillPopupOpen || SkillPopupItems.Count == 0) return;
        var next = SkillPopupIndex + delta;
        if (next < 0) next = SkillPopupItems.Count - 1;
        if (next >= SkillPopupItems.Count) next = 0;
        SetSkillPopupIndex(next);
    }

    /// <summary>确认选中当前高亮技能，并清掉输入框中的 / 前缀。</summary>
    public void CommitSkillSelection()
    {
        if (!IsSkillPopupOpen || SkillPopupIndex < 0 || SkillPopupIndex >= SkillPopupItems.Count) return;
        ApplySkill(SkillPopupItems[SkillPopupIndex].Skill.Id);
        DraftInput = string.Empty;
        CloseSkillPopup();
    }

    [RelayCommand]
    private void SelectSkillFromPopup(SkillPopupItem? item)
    {
        if (item is null) return;
        ApplySkill(item.Skill.Id);
        DraftInput = string.Empty;
        CloseSkillPopup();
    }

    private void ApplySkill(string? skillId)
    {
        SelectedSkillOption = SkillOptions.FirstOrDefault(o => o.Id == skillId) ?? SafeDefaultSkillOption();
    }

    [RelayCommand]
    private void ClearSkill() => ApplySkill(null);

    public void CloseSkillPopup()
    {
        IsSkillPopupOpen = false;
        SkillPopupIndex = -1;
        SkillPopupItems.Clear();
    }

    // ---------------- 会话管理 ----------------

    [RelayCommand]
    private void NewConversation()
    {
        if (!_settingsStore.Current.IsConfigured)
        {
            _dialogs.ShowInformation("尚未配置 API Key，请先点击右上角头像进行设置。", "提示");
            OpenSettings();
            return;
        }

        var conv = new Conversation
        {
            ModelId = _settingsStore.Current.DefaultModel,
            SkillId = SelectedSkillOption?.Id,
            Title = "新对话",
        };
        _conversationStore.Conversations.Insert(0, conv);
        var vm = new ConversationViewModel(conv);
        Conversations.Insert(0, vm);
        CurrentConversation = vm;
        SaveConversations();
        DraftInput = string.Empty;
    }

    /// <summary>
    /// 删除会话（不做二次确认，确认交互由 View 层的确认对话框负责）。
    /// </summary>
    public void DeleteConversation(ConversationViewModel? conv)
    {
        if (conv is null) return;
        conv.IsRenaming = false;
        _conversationStore.Conversations.Remove(conv.Model);
        Conversations.Remove(conv);
        if (CurrentConversation == conv)
            CurrentConversation = Conversations.FirstOrDefault();
        SaveConversations();
    }

    // ---------------- 重命名 ----------------

    /// <summary>进入重命名状态：取消其它项的重命名，预填当前标题并全选。</summary>
    public void BeginRename(ConversationViewModel? conv)
    {
        if (conv is null) return;
        foreach (var c in Conversations)
        {
            if (ReferenceEquals(c, conv)) continue;
            c.IsRenaming = false;
        }
        conv.RenameDraft = conv.Title;
        conv.IsRenaming = true;
    }

    /// <summary>确认重命名：空白输入视为放弃修改，保持原标题。</summary>
    public void CommitRename(ConversationViewModel? conv)
    {
        if (conv is null || !conv.IsRenaming) return;
        var name = conv.RenameDraft.Trim();
        if (name.Length > 0)
            conv.Title = name;
        conv.IsRenaming = false;
        SaveConversations();
    }

    /// <summary>放弃重命名，保留原标题。</summary>
    public void CancelRename(ConversationViewModel? conv)
    {
        if (conv is null) return;
        conv.IsRenaming = false;
    }

    private void SaveConversations()
    {
        foreach (var c in Conversations)
            c.SyncToModel();
        _conversationStore.Save();
    }

    // ---------------- 发送 / 停止 ----------------

    private bool CanSendMessage() =>
        CurrentConversation is not null
        && !string.IsNullOrWhiteSpace(DraftInput)
        && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync()
    {
        var conv = CurrentConversation;
        if (conv is null) return;

        var text = DraftInput.Trim();
        DraftInput = string.Empty;
        CloseSkillPopup();

        // 技能提示词
        string? skillPrompt = null;
        var skill = _skillStore.Skills.FirstOrDefault(s => s.Id == conv.SkillId);
        if (skill is not null && !string.IsNullOrWhiteSpace(skill.Prompt))
            skillPrompt = skill.Prompt;

        // 用户消息
        conv.Messages.Add(new MessageViewModel("user", text));

        if (conv.Model.Messages.Count == 0)
            conv.Title = text.Length > 20 ? text[..20] + "…" : text;
        conv.SyncToModel();

        // 助手占位（流式填充）
        var placeholder = new MessageViewModel("assistant", string.Empty);
        conv.Messages.Add(placeholder);

        IsBusy = true;
        _cts = new CancellationTokenSource();
        var throttle = Stopwatch.StartNew();
        try
        {
            await foreach (var chunk in _agentService.StreamReplyAsync(conv.Model, text, skillPrompt, _cts.Token))
            {
                placeholder.AppendChunk(chunk);
                // 节流刷新 Markdown 渲染，避免逐字重排造成卡顿
                if (throttle.ElapsedMilliseconds > 200)
                {
                    placeholder.FlushRender();
                    throttle.Restart();
                }
                WeakReferenceMessenger.Default.Send(new ScrollToBottomMessage());
            }
            if (string.IsNullOrEmpty(placeholder.Content))
                placeholder.SetContent("（模型未返回内容）");
            else
                placeholder.FlushRender();
        }
        catch (OperationCanceledException)
        {
            placeholder.AppendChunk(string.IsNullOrEmpty(placeholder.Content)
                ? "（已停止生成）"
                : "\n\n（已停止生成）");
            placeholder.FlushRender();
        }
        catch (Exception ex)
        {
            placeholder.AppendChunk(string.IsNullOrEmpty(placeholder.Content)
                ? $"⚠ 调用失败：{ex.Message}"
                : $"\n\n⚠ 调用中断：{ex.Message}");
            placeholder.FlushRender();
        }
        finally
        {
            IsBusy = false;
            _cts.Dispose();
            _cts = null;
            conv.SyncToModel();
            SaveConversations();
            WeakReferenceMessenger.Default.Send(new ScrollToBottomMessage());
        }
    }

    [RelayCommand]
    private void Stop() => _cts?.Cancel();

    // ---------------- 设置 / 技能入口 ----------------

    /// <summary>打开「模型设置」并刷新 Key 状态（对话框交互经 IDialogService 抽象）。</summary>
    [RelayCommand]
    private void OpenSettings()
    {
        _dialogs.ShowSettings();
        RefreshApiKeyStatus();
    }

    /// <summary>打开「技能管理」并刷新技能选项。</summary>
    [RelayCommand]
    private void ManageSkills()
    {
        _dialogs.ShowSkills();
        RebuildSkillOptions();
    }
}
