using System.Text.Json.Serialization;

namespace MegaAgent.Core.Models;

/// <summary>一个对话会话（含完整消息历史与 MAF 线程序列化数据）。</summary>
public class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Title { get; set; } = "新对话";

    /// <summary>使用的模型 id，如 deepseek-chat。</summary>
    public string ModelId { get; set; } = "deepseek-chat";

    /// <summary>关联的技能 Id，null 表示不使用技能。</summary>
    public string? SkillId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<ChatMessageItem> Messages { get; set; } = new();

    /// <summary>MAF AgentThread 的序列化 JSON，用于跨进程恢复多轮上下文。</summary>
    [JsonIgnore]
    public string? ThreadJson { get; set; }
}
