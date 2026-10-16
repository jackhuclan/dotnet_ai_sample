using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MegaAgent.Core.Models;
using MegaAgent.Core.Services;

namespace MegaAgent.App.ViewModels;

/// <summary>主窗口 ViewModel：会话列表、技能选择、发送/停止、设置入口。</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly SettingsStore _settingsStore;
    private readonly SkillStore _skillStore;
    private readonly ConversationStore _conversationStore;
    private readonly AgentService _agentService;
    private CancellationTokenSource? _cts;

    public ObservableCollection<ConversationViewModel> Conversations { get; } = new();

    public ObservableCollection<SkillOption> SkillOptions { get; } = new();

    public string[] AvailableModels => AppSettings.AvailableModels;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCurrentConversation))]
    private ConversationViewModel? _currentConversation;

    [ObservableProperty]
    private SkillOption? _selectedSkillOption;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string _draftInput = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private bool _isBusy;

    public bool HasCurrentConversation => CurrentConversation is not null;

    /// <summary>左下角用户区显示的 API Key 状态文案。</summary>
    [ObservableProperty]
    private string _apiKeyStatus = string.Empty;

    public void RefreshApiKeyStatus()
    {
        ApiKeyStatus = _settingsStore.Current.IsConfigured
            ? "API Key 已配置"
            : "未配置 API Key（点击设置）";
    }

    public MainViewModel(
        SettingsStore settingsStore,
        SkillStore skillStore,
        ConversationStore conversationStore,
        AgentService agentService)
    {
        _settingsStore = settingsStore;
        _skillStore = skillStore;
        _conversationStore = conversationStore;
        _agentService = agentService;

        foreach (var c in _conversationStore.Conversations)
            Conversations.Add(new ConversationViewModel(c));

        RebuildSkillOptions();
        RefreshApiKeyStatus();
        CurrentConversation = Conversations.FirstOrDefault();
    }

    partial void OnCurrentConversationChanged(ConversationViewModel? value)
    {
        // 切换会话时同步技能下拉框
        if (value is not null)
            SelectedSkillOption = SkillOptions.FirstOrDefault(o => o.Id == value.SkillId) ?? SkillOptions[0];
        SendMessageCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSkillOptionChanged(SkillOption? value)
    {
        if (CurrentConversation is not null && value is not null)
        {
            CurrentConversation.SkillId = value.Id;
            SaveConversations();
        }
    }

    public void RebuildSkillOptions()
    {
        var keepId = SelectedSkillOption?.Id;
        SkillOptions.Clear();
        SkillOptions.Add(new SkillOption(null, "不使用技能"));
        foreach (var s in _skillStore.Skills)
            SkillOptions.Add(new SkillOption(s.Id, s.Name));
        SelectedSkillOption = SkillOptions.FirstOrDefault(o => o.Id == keepId) ?? SkillOptions[0];
    }

    // ---------------- 会话管理 ----------------

    [RelayCommand]
    private void NewConversation()
    {
        if (!_settingsStore.Current.IsConfigured)
        {
            MessageBox.Show("尚未配置 API Key，请先点击左下角用户头像进行设置。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            WeakReferenceMessenger.Default.Send(new OpenSettingsMessage());
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

    [RelayCommand]
    private void DeleteConversation(ConversationViewModel? conv)
    {
        if (conv is null) return;
        if (MessageBox.Show($"确定删除对话「{conv.Title}」吗？", "确认删除",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        var model = conv.Model;
        _conversationStore.Conversations.Remove(model);
        Conversations.Remove(conv);
        if (CurrentConversation == conv)
            CurrentConversation = Conversations.FirstOrDefault();
        SaveConversations();
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

        // 技能提示词
        string? skillPrompt = null;
        var skill = _skillStore.Skills.FirstOrDefault(s => s.Id == conv.SkillId);
        if (skill is not null && !string.IsNullOrWhiteSpace(skill.Prompt))
            skillPrompt = skill.Prompt;

        // 用户消息
        conv.Messages.Add(new MessageViewModel("user", text));

        // 首条消息设置标题
        if (conv.Model.Messages.Count == 0)
            conv.Title = text.Length > 20 ? text[..20] + "…" : text;
        conv.SyncToModel();

        // 助手占位（流式填充）
        var placeholder = new MessageViewModel("assistant", string.Empty);
        conv.Messages.Add(placeholder);

        IsBusy = true;
        _cts = new CancellationTokenSource();
        try
        {
            await foreach (var chunk in _agentService.StreamReplyAsync(conv.Model, text, skillPrompt, _cts.Token))
            {
                placeholder.Content += chunk;
                WeakReferenceMessenger.Default.Send(new ScrollToBottomMessage());
            }
            if (string.IsNullOrEmpty(placeholder.Content))
                placeholder.Content = "（模型未返回内容）";
        }
        catch (OperationCanceledException)
        {
            placeholder.Content = string.IsNullOrEmpty(placeholder.Content)
                ? "（已停止生成）"
                : placeholder.Content + "\n\n（已停止生成）";
        }
        catch (Exception ex)
        {
            placeholder.Content = string.IsNullOrEmpty(placeholder.Content)
                ? $"⚠ 调用失败：{ex.Message}"
                : placeholder.Content + $"\n\n⚠ 调用中断：{ex.Message}";
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
    private void Stop()
    {
        _cts?.Cancel();
    }

    // ---------------- 技能 / 设置入口 ----------------

    [RelayCommand]
    private void OpenSettings() => WeakReferenceMessenger.Default.Send(new OpenSettingsMessage());

    [RelayCommand]
    private void ManageSkills() => WeakReferenceMessenger.Default.Send(new OpenSkillsMessage());
}
