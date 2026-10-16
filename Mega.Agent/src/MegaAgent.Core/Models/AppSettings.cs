using System.Text.Json.Serialization;

namespace MegaAgent.Core.Models;

/// <summary>应用级配置（API Key 等），存于 config.json。</summary>
public class AppSettings
{
    public const string DefaultEndpoint = "https://api.deepseek.com/v1";

    public static readonly string[] AvailableModels = ["deepseek-chat", "deepseek-reasoner"];

    public string ApiKey { get; set; } = string.Empty;

    public string Endpoint { get; set; } = DefaultEndpoint;

    public string DefaultModel { get; set; } = "deepseek-chat";

    [JsonIgnore]
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
