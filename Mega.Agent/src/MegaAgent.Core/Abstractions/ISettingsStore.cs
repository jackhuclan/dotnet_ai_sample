using MegaAgent.Core.Models;

namespace MegaAgent.Core.Abstractions;

/// <summary>
/// 应用配置（config.json）读写契约。
/// 通过接口注入，便于替换实现（如内存实现、测试替身、云端配置中心）。
/// </summary>
public interface ISettingsStore
{
    /// <summary>当前配置（内存态）。</summary>
    AppSettings Current { get; }

    /// <summary>从磁盘加载配置；文件不存在时写入内置默认配置。</summary>
    void Load();

    /// <summary>把当前配置写回磁盘。</summary>
    void Save();
}
