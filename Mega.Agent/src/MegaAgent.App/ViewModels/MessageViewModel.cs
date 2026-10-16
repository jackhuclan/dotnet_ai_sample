using CommunityToolkit.Mvvm.ComponentModel;
using MegaAgent.Core.Models;

namespace MegaAgent.App.ViewModels;

/// <summary>聊天消息的 UI 包装。流式期间逐块累积原文，按节流刷新供 Markdown 渲染。</summary>
public partial class MessageViewModel : ObservableObject
{
    public string Role { get; }

    /// <summary>原始累积内容（流式期间不断追加，不直接绑定 UI）。</summary>
    public string Content { get; private set; }

    /// <summary>渲染用内容（绑定到 Markdown 视图，节流刷新以保证流畅）。</summary>
    [ObservableProperty]
    private string _rendered;

    public DateTime Timestamp { get; }

    public bool IsUser => Role == "user";
    public bool IsAssistant => Role == "assistant";

    public MessageViewModel(ChatMessageItem item)
    {
        Role = item.Role;
        Content = item.Content;
        Rendered = item.Content;
        Timestamp = item.Timestamp;
    }

    public MessageViewModel(string role, string content)
    {
        Role = role;
        Content = content;
        Rendered = content;
        Timestamp = DateTime.Now;
    }

    public void AppendChunk(string chunk) => Content += chunk;

    public void SetContent(string content)
    {
        Content = content;
        Rendered = content;
    }

    public void FlushRender() => Rendered = Content;

    public ChatMessageItem ToModel() => new() { Role = Role, Content = Content, Timestamp = Timestamp };
}
