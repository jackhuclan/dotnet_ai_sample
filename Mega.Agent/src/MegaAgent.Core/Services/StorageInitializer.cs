using System.Diagnostics;
using MegaAgent.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace MegaAgent.Core.Services;

/// <summary>
/// 启动期存储初始化：统一触发三类持久化数据的加载。
/// 依赖通过构造函数注入，由 DI 容器在应用启动时解析并调用一次。
/// </summary>
public sealed class StorageInitializer : IStorageInitializer
{
    private readonly ISettingsStore _settings;
    private readonly ISkillStore _skills;
    private readonly IConversationStore _conversations;
    private readonly ILogger<StorageInitializer> _logger;

    public StorageInitializer(
        ISettingsStore settings,
        ISkillStore skills,
        IConversationStore conversations,
        ILogger<StorageInitializer> logger)
    {
        _settings = settings;
        _skills = skills;
        _conversations = conversations;
        _logger = logger;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("开始加载持久化数据（{Dir}）", StoragePaths.AppDataDir);

        // 配置最先加载：技能与会话的模型名回落都要用到默认模型
        _settings.Load();
        _skills.Load();
        _conversations.Load();

        _logger.LogInformation("持久化数据加载完成：技能 {SkillCount} 个，会话 {ConversationCount} 条，耗时 {Elapsed} ms",
            _skills.Skills.Count, _conversations.Conversations.Count, sw.ElapsedMilliseconds);
    }
}
