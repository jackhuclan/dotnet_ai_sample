using MegaAgent.Core.Models;

namespace MegaAgent.Core.Abstractions;

/// <summary>技能库持久化契约。</summary>
public interface ISkillStore
{
    /// <summary>技能列表（内存态）。</summary>
    List<Skill> Skills { get; set; }

    /// <summary>从磁盘加载技能库；文件不存在时预置示例技能并落盘。</summary>
    void Load();

    /// <summary>把技能列表写回磁盘。</summary>
    void Save();
}
