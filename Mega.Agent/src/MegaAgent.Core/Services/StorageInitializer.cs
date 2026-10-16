using MegaAgent.Core.Abstractions;

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

    public StorageInitializer(
        ISettingsStore settings,
        ISkillStore skills,
        IConversationStore conversations)
    {
        _settings = settings;
        _skills = skills;
        _conversations = conversations;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        // 配置最先加载：技能与会话的模型名回落都要用到默认模型
        _settings.Load();
        _skills.Load();
        _conversations.Load();
    }
}
