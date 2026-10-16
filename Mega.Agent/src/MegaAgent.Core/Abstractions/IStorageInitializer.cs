namespace MegaAgent.Core.Abstractions;

/// <summary>
/// 启动期存储初始化：按顺序加载配置文件、技能库与会话列表。
/// 由组合根（App）在构建主窗口之前调用一次，让「加载」这件事保持显式、可测试。
/// </summary>
public interface IStorageInitializer
{
    void Initialize();
}
