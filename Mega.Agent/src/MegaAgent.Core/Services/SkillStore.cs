using System.Text.Json;
using MegaAgent.Core.Abstractions;
using MegaAgent.Core.Models;
using Microsoft.Extensions.Logging;

namespace MegaAgent.Core.Services;

/// <summary>技能库持久化服务。首次运行时预置两个示例技能。</summary>
public class SkillStore : ISkillStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly ILogger<SkillStore> _logger;

    public SkillStore(ILogger<SkillStore> logger) => _logger = logger;

    public List<Skill> Skills { get; set; } = new();

    public void Load()
    {
        StoragePaths.EnsureDir();
        if (File.Exists(StoragePaths.SkillsPath))
        {
            try
            {
                var json = File.ReadAllText(StoragePaths.SkillsPath);
                Skills = JsonSerializer.Deserialize<List<Skill>>(json) ?? new List<Skill>();
                _logger.LogInformation("技能库已加载：{Count} 个技能（{Path}）", Skills.Count, StoragePaths.SkillsPath);
                return;
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                _logger.LogError(ex, "技能库解析失败，回落内置示例技能：{Path}", StoragePaths.SkillsPath);
            }
        }
        else
        {
            _logger.LogInformation("未找到技能库文件，写入内置示例技能：{Path}", StoragePaths.SkillsPath);
        }

        // 首次运行（或文件损坏）预置示例技能
        Skills =
        [
            new Skill
            {
                Name = "翻译助手",
                Description = "中英互译，保持原文风格",
                Prompt = "你是一名专业翻译。用户输入中文时翻译成英文，输入英文时翻译成中文。" +
                         "保留原文的语气与风格，只输出译文，不要解释。",
            },
            new Skill
            {
                Name = "代码审查",
                Description = "从正确性、可读性、性能三方面审查代码",
                Prompt = "你是一名资深代码审查专家。对用户给出的代码，从正确性、可读性、性能三个维度逐条给出审查意见，" +
                         "每条意见注明严重程度（高/中/低），最后给出总体结论。",
            },
        ];
        Save();
    }

    public void Save()
    {
        StoragePaths.EnsureDir();
        try
        {
            File.WriteAllText(StoragePaths.SkillsPath, JsonSerializer.Serialize(Skills, JsonOptions));
            _logger.LogDebug("技能库已保存：{Count} 个技能", Skills.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "技能库保存失败：{Path}", StoragePaths.SkillsPath);
            throw;
        }
    }
}
