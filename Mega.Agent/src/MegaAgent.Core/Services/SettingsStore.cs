using System.Text.Json;
using MegaAgent.Core.Abstractions;
using MegaAgent.Core.Models;

namespace MegaAgent.Core.Services;

/// <summary>应用配置持久化服务（config.json）。</summary>
public class SettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public AppSettings Current { get; private set; } = new();

    /// <summary>加载配置；若不存在则用内置默认 Key 初始化（开箱即用）。</summary>
    public void Load()
    {
        StoragePaths.EnsureDir();
        if (File.Exists(StoragePaths.ConfigPath))
        {
            var json = File.ReadAllText(StoragePaths.ConfigPath);
            Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        else
        {
            Current = new AppSettings
            {
                ApiKey = "sk-bdb787f4dae940e39b4f20ed8d045ad0",
                Endpoint = AppSettings.DefaultEndpoint,
                DefaultModel = AppSettings.AvailableModels[0],
            };
            Save();
            return;
        }

        // 历史配置里的模型名可能已不在可选列表中，回落到第一个可用模型
        if (!AppSettings.AvailableModels.Contains(Current.DefaultModel))
        {
            Current.DefaultModel = AppSettings.AvailableModels[0];
            Save();
        }
    }

    public void Save()
    {
        StoragePaths.EnsureDir();
        File.WriteAllText(StoragePaths.ConfigPath, JsonSerializer.Serialize(Current, JsonOptions));
    }
}
