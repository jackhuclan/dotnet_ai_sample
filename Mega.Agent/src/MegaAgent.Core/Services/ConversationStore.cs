using System.Text.Json;
using MegaAgent.Core.Models;

namespace MegaAgent.Core.Services;

/// <summary>基于 JSON 文件的会话持久化服务。</summary>
public class ConversationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public List<Conversation> Conversations { get; private set; } = new();

    public void Load()
    {
        StoragePaths.EnsureDir();
        if (File.Exists(StoragePaths.ConversationsPath))
        {
            var json = File.ReadAllText(StoragePaths.ConversationsPath);
            Conversations = JsonSerializer.Deserialize<List<Conversation>>(json) ?? new List<Conversation>();
        }
    }

    public void Save()
    {
        StoragePaths.EnsureDir();
        File.WriteAllText(StoragePaths.ConversationsPath, JsonSerializer.Serialize(Conversations, JsonOptions));
    }
}
