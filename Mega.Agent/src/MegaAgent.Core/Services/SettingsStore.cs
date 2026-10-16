using System.Text.Json;
using MegaAgent.Core.Abstractions;
using MegaAgent.Core.Models;
using Microsoft.Extensions.Logging;

namespace MegaAgent.Core.Services;

/// <summary>应用配置持久化服务（config.json）。</summary>
public class SettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly ILogger<SettingsStore> _logger;

    public SettingsStore(ILogger<SettingsStore> logger) => _logger = logger;

    public AppSettings Current { get; private set; } = new();

    /// <summary>加载配置；若不存在或损坏则用内置默认 Key 初始化（开箱即用）。</summary>
    public void Load()
    {
        StoragePaths.EnsureDir();

        var path = StoragePaths.ConfigPath;
        if (!File.Exists(path))
        {
            _logger.LogInformation("未找到配置文件，写入默认配置：{Path}", path);
            Current = CreateDefault();
            Save();
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            _logger.LogInformation("配置已加载：{Path}（默认模型 {Model}）", path, Current.DefaultModel);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // 配置损坏不应导致应用无法启动：记录现场后用默认配置重建
            _logger.LogError(ex, "配置文件解析失败，已回落默认配置：{Path}", path);
            Current = CreateDefault();
            Save();
            return;
        }

        // 历史配置里的模型名可能已不在可选列表中，回落到第一个可用模型
        if (!AppSettings.AvailableModels.Contains(Current.DefaultModel))
        {
            _logger.LogWarning("已保存的模型 {Model} 不在可选列表中，回落到 {Fallback}",
                Current.DefaultModel, AppSettings.AvailableModels[0]);
            Current.DefaultModel = AppSettings.AvailableModels[0];
            Save();
        }
    }

    public void Save()
    {
        StoragePaths.EnsureDir();
        try
        {
            File.WriteAllText(StoragePaths.ConfigPath,
                JsonSerializer.Serialize(Current, JsonOptions));
            _logger.LogDebug("配置已保存：{Path}", StoragePaths.ConfigPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "配置保存失败：{Path}", StoragePaths.ConfigPath);
            throw;
        }
    }

    private static AppSettings CreateDefault() => new()
    {
        ApiKey = "sk-bdb787f4dae940e39b4f20ed8d045ad0",
        Endpoint = AppSettings.DefaultEndpoint,
        DefaultModel = AppSettings.AvailableModels[0],
    };
}
