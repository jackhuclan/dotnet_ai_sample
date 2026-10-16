namespace MegaAgent.Core.Services;

/// <summary>数据存储路径：应用目录下的 MegaAgent 文件夹（与 exe 同级）。</summary>
public static class StoragePaths
{
    public static string AppDataDir { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MegaAgent");

    public static string ConfigPath { get; } = Path.Combine(AppDataDir, "config.json");
    public static string ConversationsPath { get; } = Path.Combine(AppDataDir, "conversations.json");
    public static string SkillsPath { get; } = Path.Combine(AppDataDir, "skills.json");

    public static void EnsureDir()
    {
        if (!Directory.Exists(AppDataDir))
            Directory.CreateDirectory(AppDataDir);
    }
}

/// <summary>
/// 日志目录：应用目录下的 MegaAgent\logs（与 config.json 同处一个应用数据目录）。
/// 与 <see cref="StoragePaths"/> 同处一个应用数据目录，卸载时删一个文件夹即可。
/// 具体文件名 / 归档策略由 NLog.config 定义，这里只维护目录本身；
/// NLog.config 里的 logDir 必须与本属性保持一致。
/// </summary>
public static class LogPaths
{
    public static string Directory { get; } = Path.Combine(StoragePaths.AppDataDir, "logs");

    public static void EnsureDir()
    {
        if (!System.IO.Directory.Exists(Directory))
            System.IO.Directory.CreateDirectory(Directory);
    }
}
