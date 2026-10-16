using MegaAgent.Core.Models;

namespace MegaAgent.Core.Abstractions;

/// <summary>会话（历史对话）持久化契约。</summary>
public interface IConversationStore
{
    /// <summary>会话列表（内存态）。</summary>
    List<Conversation> Conversations { get; }

    /// <summary>从磁盘加载会话列表。</summary>
    void Load();

    /// <summary>把会话列表写回磁盘。</summary>
    void Save();
}
