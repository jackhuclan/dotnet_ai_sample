using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MegaAgent.Core.Abstractions;
using MegaAgent.Core.Models;
using Microsoft.Extensions.Logging;

namespace MegaAgent.App.ViewModels;

/// <summary>主窗口 ViewModel：会话列表、斜杠技能选择、发送/停止、设置入口。</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;
    private readonly ISkillStore _skillStore;
    private readonly IConversationStore _conversationStore;
    private readonly IAgentService _agentService;
    private readonly IDialogService _dialogs;
    private readonly ILogger<MainViewModel> _logger;
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

    /// <summary>
    /// 有当前会话、但该会话里还没有任何消息 → 显示「还没有消息，输入内容开始对话吧」。
    /// 与「无会话」提示互斥：无会话时 CurrentConversation 为 null，由另一条提示负责。
    /// </summary>
    public bool ShowEmptyChatHint =>
        CurrentConversation is not null && CurrentConversation.Messages.Count == 0;

    /// <summary>
    /// 输入区「模型」下拉的选中项。
    /// 有当前会话时读写该会话的模型；没有会话（冷启动 / 会话被删光）时回落到
    /// 「模型设置」里的默认模型，保证下拉框任何时候都有值、都能切换，
    /// 且新建会话会沿用这里选的模型。
    /// </summary>
    public string SelectedModel
    {
        get
        {
            var model = CurrentConversation?.ModelId ?? _settingsStore.Current.DefaultModel;
            return AppSettings.AvailableModels.Contains(model) ? model : AppSettings.AvailableModels[0];
        }
        set
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var model = AppSettings.AvailableModels.Contains(value) ? value : AppSettings.AvailableModels[0];

            if (CurrentConversation is { } conv)
            {
                if (conv.ModelId == model) return;
                conv.ModelId = model;
                _logger.LogInformation("会话 {ConversationId} 切换模型为 {Model}", conv.Model.Id, model);
                SaveConversations();
            }
            else
            {
                if (_settingsStore.Current.DefaultModel == model) return;
                _settingsStore.Current.DefaultModel = model;
                try
                {
                    _settingsStore.Save();
                }
                catch (Exception ex)
                {
                    // 落盘失败不阻断交互，仅记录
                    _logger.LogError(ex, "默认模型写盘失败：{Model}", model);
                }
                _logger.LogInformation("当前无会话，模型下拉把默认模型改为 {Model}（新建会话沿用）", model);
            }

            OnPropertyChanged();
        }
    }

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
        IAgentService agentService,
        ILogger<MainViewModel> logger)
    {
        _dialogs = dialogs;
        _settingsStore = settingsStore;
        _skillStore = skillStore;
        _conversationStore = conversationStore;
        _agentService = agentService;
        _logger = logger;

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
        {
            _logger.LogInformation("已把 {Count} 个历史会话的模型回落到 {Model}",
                _conversationStore.Conversations.Count, _settingsStore.Current.DefaultModel);
            _conversationStore.Save();
        }

        RebuildSkillOptions();
        RefreshApiKeyStatus();

        if (Conversations.Count == 0)
        {
            // 冷启动没有任何历史会话：默认预置一个空会话，用户打开应用即可直接输入
            var created = CreateConversation();
            _logger.LogInformation("冷启动未发现历史会话，已自动新建会话 {ConversationId}（模型 {Model}）",
                created.Model.Id, created.Model.ModelId);
        }
        else
        {
            CurrentConversation = Conversations.FirstOrDefault();
        }

        _logger.LogInformation("主界面就绪：历史会话 {ConversationCount} 条，可选技能 {SkillCount} 个，默认模型 {Model}",
            Conversations.Count, _skillStore.Skills.Count, _settingsStore.Current.DefaultModel);
    }

    public void RefreshApiKeyStatus()
    {
        ApiKeyStatus = _settingsStore.Current.IsConfigured
            ? "API Key 已配置"
            : "未配置 API Key";
    }

    /// <summary>
    /// 切换会话时解绑上一个会话的消息集合变更监听。
    /// 参数名必须与生成器声明一致（这里传进来的是「被替换掉」的旧会话）。
    /// </summary>
    partial void OnCurrentConversationChanging(ConversationViewModel? value)
    {
        if (value is not null)
            value.Messages.CollectionChanged -= OnCurrentMessagesChanged;
    }

    partial void OnCurrentConversationChanged(ConversationViewModel? value)
    {
        if (value is not null)
        {
            // 消息增删会改变空状态提示的显隐
            value.Messages.CollectionChanged += OnCurrentMessagesChanged;
            SelectedSkillOption = SkillOptions.FirstOrDefault(o => o.Id == value.SkillId) ?? SafeDefaultSkillOption();
        }
        OnPropertyChanged(nameof(ShowEmptyChatHint));
        OnPropertyChanged(nameof(SelectedModel));
        SendMessageCommand.NotifyCanExecuteChanged();
    }

    private void OnCurrentMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => OnPropertyChanged(nameof(ShowEmptyChatHint));

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

        var vm = CreateConversation();
        SaveConversations();

        _logger.LogInformation("已新建会话 {ConversationId}（模型 {Model}）", vm.Model.Id, vm.Model.ModelId);
    }

    /// <summary>
    /// 建一个空会话：插到列表最前、切成当前会话。
    /// 「新建对话」命令与冷启动预置会话共用这段逻辑。
    /// </summary>
    private ConversationViewModel CreateConversation()
    {
        var conv = new Conversation
        {
            ModelId = _settingsStore.Current.DefaultModel,
            SkillId = SelectedSkillOption?.Id,
            Title = "新对话",
        };
        _conversationStore.Conversations.Insert(0, conv);
        var vm = new ConversationViewModel(conv);
        Conversations.Insert(0, vm);
        DraftInput = string.Empty;
        CurrentConversation = vm;
        return vm;
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

        _logger.LogInformation("已删除会话 {ConversationId}（{Title}）", conv.Model.Id, conv.Title);
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

        try
        {
            _conversationStore.Save();
        }
        catch (Exception ex)
        {
            // 落盘失败不能中断交互：记录后由用户在日志里追溯
            _logger.LogError(ex, "会话写盘失败，本次改动仅存在于内存：{Path}",
                MegaAgent.Core.Services.StoragePaths.ConversationsPath);
        }
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
            _logger.LogInformation("用户中止了回答生成：会话 {ConversationId}，已接收 {CharCount} 字",
                conv.Model.Id, placeholder.Content.Length);
            placeholder.AppendChunk(string.IsNullOrEmpty(placeholder.Content)
                ? "（已停止生成）"
                : "\n\n（已停止生成）");
            placeholder.FlushRender();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "对话调用失败：会话 {ConversationId}，模型 {Model}",
                conv.Model.Id, conv.Model.ModelId);
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
