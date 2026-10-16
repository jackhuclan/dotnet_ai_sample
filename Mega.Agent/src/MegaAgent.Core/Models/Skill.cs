using System.Text.Json.Serialization;

namespace MegaAgent.Core.Models;

/// <summary>技能 = 系统提示词预设。</summary>
public class Skill
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "未命名技能";

    public string Description { get; set; } = string.Empty;

    /// <summary>作为 system prompt 注入 agent。</summary>
    public string Prompt { get; set; } = string.Empty;

    [JsonIgnore]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
