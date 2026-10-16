using System.ClientModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MegaAgent.Core.Models;
using MegaAgent.Core.Services;
using Microsoft.Extensions.AI;
using OpenAI;

namespace MegaAgent.App.ViewModels;

/// <summary>
/// 「模型设置」对话框 ViewModel：持有所需填写的配置、实时必填校验与保存/测试/取消命令。
/// 所有状态与判断都在这里，View 只负责绑定与关闭窗口。
/// </summary>
public partial class SettingsDialogViewModel : ObservableObject
{
    private readonly SettingsStore _store;

    public string[] AvailableModels => AppSettings.AvailableModels;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ApiKeyHasError))]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private string _apiKey;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EndpointHasError))]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private string _endpoint;

    [ObservableProperty]
    private string _selectedModel;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private bool _isTesting;

    /// <summary>API Key 为空 → 必填未满足（边框标红 + 显示提示）。</summary>
    public bool ApiKeyHasError => string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>接口地址为空 → 必填未满足。</summary>
    public bool EndpointHasError => string.IsNullOrWhiteSpace(Endpoint);

    /// <summary>必填项都已填写，且当前没有正在进行的连接测试。</summary>
    public bool CanSubmit => !ApiKeyHasError && !EndpointHasError && !IsTesting;

    /// <summary>请求 View 关闭对话框，参数为 DialogResult（null 表示仅关闭）。</summary>
    public event EventHandler<bool?>? CloseRequested;

    public SettingsDialogViewModel(SettingsStore store)
    {
        _store = store;
        _apiKey = store.Current.ApiKey ?? string.Empty;
        _endpoint = store.Current.Endpoint ?? string.Empty;
        _selectedModel = store.Current.DefaultModel is { Length: > 0 } m
                         && AppSettings.AvailableModels.Contains(m)
            ? m
            : AppSettings.AvailableModels[0];
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private void Save()
    {
        _store.Current.ApiKey = ApiKey.Trim();
        _store.Current.Endpoint = Endpoint.Trim();
        _store.Current.DefaultModel = SelectedModel;
        _store.Save();
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        StatusText = "正在测试连接…";
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var client = new OpenAIClient(new ApiKeyCredential(ApiKey.Trim()),
                new OpenAIClientOptions { Endpoint = new Uri(Endpoint.Trim()) });
            IChatClient chatClient = client.GetChatClient(SelectedModel).AsIChatClient();
            var reply = await chatClient.GetResponseAsync("ping", cancellationToken: cts.Token);
            StatusText = $"✅ 连接成功，{SelectedModel} 回复：{Truncate(reply.Text, 60)}";
        }
        catch (Exception ex)
        {
            StatusText = $"❌ 连接失败：{ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    private static string Truncate(string? text, int max)
    {
        if (string.IsNullOrEmpty(text)) return "(空)";
        return text.Length <= max ? text : text[..max] + "…";
    }
}
