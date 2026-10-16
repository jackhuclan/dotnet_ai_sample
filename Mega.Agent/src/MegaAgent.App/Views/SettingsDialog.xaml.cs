using System.ClientModel;
using System.Windows;
using MegaAgent.Core.Models;
using MegaAgent.Core.Services;
using Microsoft.Extensions.AI;
using OpenAI;

namespace MegaAgent.App;

public partial class SettingsDialog : Window
{
    private readonly SettingsStore _store;
    private readonly RequiredField _apiKeyField;
    private readonly RequiredField _endpointField;

    public SettingsDialog(SettingsStore store)
    {
        InitializeComponent();
        _store = store;

        ApiKeyBox.Password = store.Current.ApiKey;
        EndpointBox.Text = store.Current.Endpoint;
        ModelCombo.ItemsSource = AppSettings.AvailableModels;
        ModelCombo.SelectedItem = store.Current.DefaultModel is { Length: > 0 } m
            && AppSettings.AvailableModels.Contains(m)
                ? m
                : AppSettings.AvailableModels[0];

        // API Key 与接口地址为必填：为空时标红 + 显示「必填」提示
        _apiKeyField = new RequiredField(ApiKeyBox, ApiKeyError, "API Key", () => ApiKeyBox.Password);
        _endpointField = new RequiredField(EndpointBox, EndpointError, "接口地址", () => EndpointBox.Text);

        // 用户重新编辑时清除错误提示（hc:PasswordBox 无 PasswordChanged 事件，用焦点/按键事件代替）
        ApiKeyBox.GotFocus += (_, _) => _apiKeyField.Clear();
        ApiKeyBox.PreviewKeyUp += (_, _) => _apiKeyField.Clear();
        EndpointBox.TextChanged += (_, _) => _endpointField.Clear();
    }

    /// <summary>校验必填项；全部通过返回 true。</summary>
    private bool ValidateRequired()
    {
        var keyOk = _apiKeyField.Validate();
        var endpointOk = _endpointField.Validate();

        if (!keyOk) ApiKeyBox.Focus();
        else if (!endpointOk) EndpointBox.Focus();

        return keyOk && endpointOk;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateRequired()) return;

        _store.Current.ApiKey = ApiKeyBox.Password.Trim();
        _store.Current.Endpoint = EndpointBox.Text.Trim();
        _store.Current.DefaultModel = ModelCombo.SelectedItem as string ?? AppSettings.AvailableModels[0];
        _store.Save();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void TestButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateRequired())
        {
            StatusText.Text = "请先补全必填项。";
            return;
        }

        var key = ApiKeyBox.Password.Trim();
        var endpoint = EndpointBox.Text.Trim();
        var model = ModelCombo.SelectedItem as string ?? AppSettings.AvailableModels[0];

        TestButton.IsEnabled = false;
        StatusText.Text = "正在测试连接…";
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var client = new OpenAIClient(new ApiKeyCredential(key),
                new OpenAIClientOptions { Endpoint = new Uri(endpoint) });
            IChatClient chatClient = client.GetChatClient(model).AsIChatClient();
            var reply = await chatClient.GetResponseAsync("ping", cancellationToken: cts.Token);
            StatusText.Text = $"✅ 连接成功，{model} 回复：{Truncate(reply.Text, 60)}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"❌ 连接失败：{ex.Message}";
        }
        finally
        {
            TestButton.IsEnabled = true;
        }
    }

    private static string Truncate(string? text, int max)
    {
        if (string.IsNullOrEmpty(text)) return "(空)";
        return text.Length <= max ? text : text[..max] + "…";
    }
}
