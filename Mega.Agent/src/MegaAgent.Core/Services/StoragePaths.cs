namespace MegaAgent.Core.Services;

/// <summary>数据存储路径：%APPDATA%\MegaAgent</summary>
public static class StoragePaths
{
    public static string AppDataDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MegaAgent");

    public static string ConfigPath { get; } = Path.Combine(AppDataDir, "config.json");
    public static string ConversationsPath { get; } = Path.Combine(AppDataDir, "conversations.json");
    public static string SkillsPath { get; } = Path.Combine(AppDataDir, "skills.json");

    public static void EnsureDir()
    {
        if (!Directory.Exists(AppDataDir))
            Directory.CreateDirectory(AppDataDir);
    }
}
