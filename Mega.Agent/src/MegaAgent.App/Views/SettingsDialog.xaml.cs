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
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _store.Current.ApiKey = ApiKeyBox.Password.Trim();
        _store.Current.Endpoint = string.IsNullOrWhiteSpace(EndpointBox.Text)
            ? AppSettings.DefaultEndpoint
            : EndpointBox.Text.Trim();
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
        var key = ApiKeyBox.Password.Trim();
        var endpoint = string.IsNullOrWhiteSpace(EndpointBox.Text)
            ? AppSettings.DefaultEndpoint
            : EndpointBox.Text.Trim();
        var model = ModelCombo.SelectedItem as string ?? AppSettings.AvailableModels[0];

        if (string.IsNullOrWhiteSpace(key))
        {
            StatusText.Text = "请先填写 API Key。";
            return;
        }

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
