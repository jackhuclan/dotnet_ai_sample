using CommunityToolkit.Mvvm.ComponentModel;
using MegaAgent.Core.Models;

namespace MegaAgent.App.ViewModels;

/// <summary>聊天消息的 UI 包装。</summary>
public partial class MessageViewModel : ObservableObject
{
    public string Role { get; }

    [ObservableProperty]
    private string _content;

    public DateTime Timestamp { get; }

    public bool IsUser => Role == "user";
    public bool IsAssistant => Role == "assistant";

    public MessageViewModel(ChatMessageItem item)
    {
        Role = item.Role;
        Content = item.Content;
        Timestamp = item.Timestamp;
    }

    public MessageViewModel(string role, string content)
    {
        Role = role;
        Content = content;
        Timestamp = DateTime.Now;
    }

    public ChatMessageItem ToModel() => new() { Role = Role, Content = Content, Timestamp = Timestamp };
}
