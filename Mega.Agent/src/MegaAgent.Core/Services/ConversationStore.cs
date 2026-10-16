using System.Text.Json;
using MegaAgent.Core.Abstractions;
using MegaAgent.Core.Models;
using Microsoft.Extensions.Logging;

namespace MegaAgent.Core.Services;

/// <summary>基于 JSON 文件的会话持久化服务。</summary>
public class ConversationStore : IConversationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly ILogger<ConversationStore> _logger;

    public ConversationStore(ILogger<ConversationStore> logger) => _logger = logger;

    public List<Conversation> Conversations { get; private set; } = new();

    public void Load()
    {
        StoragePaths.EnsureDir();
        if (!File.Exists(StoragePaths.ConversationsPath))
        {
            _logger.LogInformation("未找到会话文件，以空会话列表启动：{Path}", StoragePaths.ConversationsPath);
            return;
        }

        try
        {
            var json = File.ReadAllText(StoragePaths.ConversationsPath);
            Conversations = JsonSerializer.Deserialize<List<Conversation>>(json) ?? new List<Conversation>();
            _logger.LogInformation("会话已加载：{Count} 条（{Path}）", Conversations.Count, StoragePaths.ConversationsPath);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // 读取失败不应阻塞启动：保留空列表，原文件保持不动以便人工恢复
            _logger.LogError(ex, "会话文件解析失败，本次以空会话列表启动：{Path}", StoragePaths.ConversationsPath);
            Conversations = new List<Conversation>();
        }
    }

    public void Save()
    {
        StoragePaths.EnsureDir();
        try
        {
            File.WriteAllText(StoragePaths.ConversationsPath,
                JsonSerializer.Serialize(Conversations, JsonOptions));
            _logger.LogDebug("会话已保存：{Count} 条", Conversations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "会话保存失败：{Path}", StoragePaths.ConversationsPath);
            throw;
        }
    }
}
